using System.Diagnostics.CodeAnalysis;

namespace EBAY.Shared;

/// <summary>
/// Minimo interface para el error.-
/// </summary>
public interface IError
{
    public string MsgErr { get; }
}

public abstract class ErrorBase : IError
{
    public string MsgErr { get; set; }

    protected ErrorBase()
    {

    }
    protected ErrorBase(string mensaje)
    {
        this.MsgErr = mensaje;
    }
    protected ErrorBase(Exception ex)
    {
        var mensajes = new List<string>();
        Exception? _ex = ex;
        while (_ex is not null)
        {
            mensajes.Add($"Excepcion:{_ex.Source}/{_ex.TargetSite}:{_ex.Message}");
            _ex = _ex.InnerException;
        }
        MsgErr = string.Join(' ', mensajes);
    }




}

public class MiError : ErrorBase
{

    private MiError() : base()
    {

    }
    private MiError(string mensaje) : base(mensaje)
    {

    }
    private MiError(Exception ex) : base(ex)
    {

    }

    public static async Task<MiError> FromHttpResponse(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode) throw new ArgumentException("Response is Ok");
        var msg = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        return new MiError($"Status:{resp.StatusCode},Reason:{resp.ReasonPhrase},Detail:{msg}");
    }
    public static MiError FromException(Exception ex) => new MiError(ex);

    public static MiError FromMsg(string msg) => new MiError(msg);
}




/// <summary>
/// Equivalente funcional de Rust Result<T, E>.
/// </summary>
public readonly record struct Result<TValue, TError> where TError : IError
{
    private readonly TValue? _value;
    private readonly TError? _error;

    public bool IsOk { get; }
    public bool IsErr => !IsOk;

    public TValue Value =>
        IsOk
            ? _value!
            : throw new InvalidOperationException(
                $"Cannot access {nameof(Value)} on an Err result.");

    public TError Error =>
        IsErr
            ? _error!
            : throw new InvalidOperationException(
                $"Cannot access {nameof(Error)} on an Ok result.");

    private Result(TValue value)
    {
        IsOk = true;
        _value = value;
        _error = default;
    }

    private Result(TError error)
    {
        IsOk = false;
        _error = error;
        _value = default;
    }

    public static Result<TValue, TError> Ok(TValue value)
        => new(value);

    public static Result<TValue, TError> Err(TError error)
        => new(error);

    public static implicit operator Result<TValue, TError>(TValue value)
        => Ok(value);

    public static implicit operator Result<TValue, TError>(TError error)
        => Err(error);

    public bool TryGetValue(
        [NotNullWhen(true)] out TValue? value)
    {
        value = IsOk ? _value : default;
        return IsOk;
    }

    public bool TryGetError(
        [NotNullWhen(true)] out TError? error)
    {
        error = IsErr ? _error : default;
        return IsErr;
    }

    // =========================================================
    // MAP
    // =========================================================
    /// <summary>
    /// Cambia un Result a otro tipo de Result con 
    /// </summary>
    /// <example>
    /// Result<string, UserError> result = GetUser(1).Map(user => user.Name);  
    /// </example>
    /// <typeparam name="TOut"></typeparam>
    /// <param name="mapper"></param>
    /// <returns></returns>
    public Result<TOut, TError> Map<TOut>(
        Func<TValue, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return IsOk
            ? Result<TOut, TError>.Ok(mapper(Value))
            : Result<TOut, TError>.Err(Error);
    }

    public async Task<Result<TOut, TError>> MapAsync<TOut>(
        Func<TValue, Task<TOut>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        if (IsErr)
            return Result<TOut, TError>.Err(Error);

        return Result<TOut, TError>.Ok(
            await mapper(Value));
    }

    // =========================================================
    // BIND (flatMap)
    // =========================================================


    /// <summary>
    /// Esto es para continuar el result
    /// </summary>
    /// <example>
    /// Result<User, UserError> GetUser(int id);
    /// Result<Account, UserError> GetAccount(User user);
    /// var result = GetUser(1).Bind(GetAccount);
    /// </example>
    /// <typeparam name="TOut"></typeparam>
    /// <param name="binder"></param>
    /// <returns></returns>
    public Result<TOut, TError> Bind<TOut>(
        Func<TValue, Result<TOut, TError>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return IsOk
            ? binder(Value)
            : Result<TOut, TError>.Err(Error);
    }
    /// <summary>
    /// Esto es para continuar con un neuvo result async.
    /// </summary>
    /// <example>
    /// var result = await GetUser(1).BindAsync(GetAccountAsync).ConfigureAwait(false);
    /// </example>
    /// 
    /// <typeparam name="TOut"></typeparam>
    /// <param name="binder"></param>
    /// <returns></returns>
    public async Task<Result<TOut, TError>> BindAsync<TOut>(
        Func<TValue, Task<Result<TOut, TError>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (IsErr)
            return Result<TOut, TError>.Err(Error);

        return await binder(Value);
    }

    // =========================================================
    // MATCH
    // =========================================================
    /// <summary>
    /// Devuelve un resultado
    /// </summary>
    /// <example>
    /// string response = GetUser(5).Match(
    ///    ok: user => $"User: {user.Name}",
    ///    err: error => $"Error: {error}");
    /// </example>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="ok"></param>
    /// <param name="err"></param>
    /// <returns></returns>
    public TResult Match<TResult>(
        Func<TValue, TResult> ok,
        Func<TError, TResult> err)
    {
        ArgumentNullException.ThrowIfNull(ok);
        ArgumentNullException.ThrowIfNull(err);

        return IsOk
            ? ok(Value)
            : err(Error);
    }
    /// <summary>
    ///  string response = await result.MatchAsync(
    ///   ok: account => Task.FromResult($"Id={account.Id}"),
    ///   err: error => Task.FromResult(error.ToString()));
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="ok"></param>
    /// <param name="err"></param>
    /// <returns></returns>
    public Task<TResult> MatchAsync<TResult>(
        Func<TValue, Task<TResult>> ok,
        Func<TError, Task<TResult>> err)
    {
        ArgumentNullException.ThrowIfNull(ok);
        ArgumentNullException.ThrowIfNull(err);

        return IsOk
            ? ok(Value)
            : err(Error);
    }

    // =========================================================
    // TAP
    // =========================================================

    /// <summary>
    /// Opera una accion sobre TValue si IsOk
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public Result<TValue, TError> Tap(
        Action<TValue> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsOk)
            action(Value);

        return this;
    }
    /// <summary>
    /// Opera un Task sobre TValue 
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public async Task<Result<TValue, TError>> TapAsync(
        Func<TValue, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsOk)
            await action(Value);

        return this;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    public TValue Unwrap()
        => Value;

    public TValue UnwrapOr(TValue fallback)
        => IsOk ? Value : fallback;

    public TValue UnwrapOrElse(
        Func<TError, TValue> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return IsOk
            ? Value
            : factory(Error);
    }

    public override string ToString()
    {
        return IsOk
            ? $"Ok({Value})"
            : $"Err({Error})";
    }
}


public static class ResultExtensions
{
    public static async Task<Result<TOut, TError>>
        MapAsync<TIn, TOut, TError>(
            this Task<Result<TIn, TError>> task,
            Func<TIn, TOut> mapper) where TError : IError
    {
        var result = await task;

        return result.Map(mapper);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <example>
    /// var result =
    /// await repository.GetUserAsync(id)
    ///    .BindAsync(accountService.LoadAccountAsync)
    ///    .BindAsync(validationService.ValidateAsync)
    ///    .MapAsync(model => model.Name);
    /// 
    /// </example>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    /// <typeparam name="TError"></typeparam>
    /// <param name="task"></param>
    /// <param name="binder"></param>
    /// <returns></returns>
    public static async Task<Result<TOut, TError>>
        BindAsync<TIn, TOut, TError>(
            this Task<Result<TIn, TError>> task,
            Func<TIn, Task<Result<TOut, TError>>> binder) where TError : IError
    {
        var result = await task;

        return await result.BindAsync(binder);
    }

    public static async Task<TResult>
        MatchAsync<TValue, TError, TResult>(
            this Task<Result<TValue, TError>> task,
            Func<TValue, TResult> ok,
            Func<TError, TResult> err) where TError : IError
    {
        var result = await task;

        return result.Match(ok, err);
    }
}
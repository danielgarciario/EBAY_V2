using Microsoft.Extensions.Logging;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;

namespace EBAY.TRADING.API.CLIENT.Inspectors;

public class SoapLoggerInspector : IClientMessageInspector
{
    private readonly ILogger log;

    public SoapLoggerInspector(ILogger log)
    {
        this.log = log;

    }
    public object BeforeSendRequest(ref Message request, IClientChannel channel)
    {
        var copy = request.CreateBufferedCopy(int.MaxValue);

        var msgToLog = copy.CreateMessage();
        request = copy.CreateMessage();

        var sb = new StringBuilder();

        using (var sw = new StringWriter(sb))
        using (var xw = XmlWriter.Create(sw, new XmlWriterSettings
        {
            Indent = true
        }))
        {
            msgToLog.WriteMessage(xw);
        }

        log.LogInformation("SOAP REQUEST:");
        log.LogInformation(sb.ToString());

        return null;
    }

    public void AfterReceiveReply(ref Message reply, object correlationState)
    {
    }
}
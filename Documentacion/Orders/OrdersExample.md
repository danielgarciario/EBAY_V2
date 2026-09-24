# Ejemplo de Respuesta a GetOrders real:

## Documentacion:

[EBAY GetOrders Fulfillment API](https://developer.ebay.com/develop/api/sell/fulfillment_api#sell-fulfillment_api-order-getorders)
[EBAY GetOrer Fullfillment API](https://developer.ebay.com/develop/api/sell/fulfillment_api#sell-fulfillment_api-order-getorder)


## Query:

```bash
curl --request GET \
  --url 'https://api.ebay.com/sell/fulfillment/v1/order?fieldGroups=TAX_BREAKDOWN&limit=5'
```

## Response

```json
{
  "href": "https://api.ebay.com/sell/fulfillment/v1/order?limit=5&offset=0",
  "total": 480,
  "next": "https://api.ebay.com/sell/fulfillment/v1/order?limit=5&offset=5",
  "limit": 5,
  "offset": 0,
  "orders": [
    {
      "orderId": "06-15211-22068",
      "legacyOrderId": "06-15211-22068",
      "creationDate": "2026-09-23T09:59:05.000Z",
      "lastModifiedDate": "2026-09-23T09:59:08.000Z",
      "orderFulfillmentStatus": "NOT_STARTED",
      "orderPaymentStatus": "PAID",
      "sellerId": "handwerker3000_de",
      "buyer": {
        "username": "kfz_profi123",
        "taxAddress": {
          "city": "Arnsberg",
          "postalCode": "59821",
          "countryCode": "DE"
        },
        "buyerRegistrationAddress": {
          "contactAddress": {
            "addressLine1": "Ringstr.209",
            "city": "Arnsberg",
            "postalCode": "59821",
            "countryCode": "DE"
          },
          "primaryPhone": {
            "phoneNumber": "17678023477"
          },
          "email": "492d7185f9590bae2d97@members.ebay.com"
        }
      },
      "pricingSummary": {
        "priceSubtotal": {
          "value": "70.33",
          "currency": "EUR"
        },
        "deliveryCost": {
          "value": "4.95",
          "currency": "EUR"
        },
        "total": {
          "value": "75.28",
          "currency": "EUR"
        }
      },
      "cancelStatus": {
        "cancelState": "NONE_REQUESTED",
        "cancelRequests": []
      },
      "paymentSummary": {
        "totalDueSeller": {
          "value": "63.09",
          "currency": "EUR"
        },
        "refunds": [],
        "payments": [
          {
            "paymentMethod": "EBAY",
            "paymentReferenceId": "420004_S",
            "paymentDate": "2026-09-23T09:59:05.583Z",
            "amount": {
              "value": "63.09",
              "currency": "EUR"
            },
            "paymentStatus": "PAID"
          }
        ]
      },
      "fulfillmentStartInstructions": [
        {
          "fulfillmentInstructionsType": "SHIP_TO",
          "minEstimatedDeliveryDate": "2026-09-23T22:00:00.000Z",
          "maxEstimatedDeliveryDate": "2026-09-24T22:00:00.000Z",
          "ebaySupportedFulfillment": false,
          "shippingStep": {
            "shipTo": {
              "fullName": "Ludmilla Gorski",
              "contactAddress": {
                "addressLine1": "Am Kreuzkamp 3",
                "city": "Arnsberg",
                "postalCode": "59821",
                "countryCode": "DE"
              },
              "primaryPhone": {
                "phoneNumber": "4915738371880"
              },
              "email": "492d7185f9590bae2d97@members.ebay.com"
            },
            "shippingCarrierCode": "DPD",
            "shippingServiceCode": "DE_DPDClassic"
          }
        }
      ],
      "fulfillmentHrefs": [],
      "lineItems": [
        {
          "lineItemId": "10085010063106",
          "legacyItemId": "406355941732",
          "legacyVariationId": "676681368422",
          "variationAspects": [
            {
              "name": "Länge",
              "value": "1600 mm"
            }
          ],
          "sku": "122626.St",
          "title": "Motive Fassaden Schleifraspel für Styropor Schlefbrett für z.B. Dämmplatten",
          "lineItemCost": {
            "value": "70.33",
            "currency": "EUR"
          },
          "quantity": 1,
          "soldFormat": "FIXED_PRICE",
          "listingMarketplaceId": "EBAY_DE",
          "purchaseMarketplaceId": "EBAY_DE",
          "lineItemFulfillmentStatus": "NOT_STARTED",
          "total": {
            "value": "75.28",
            "currency": "EUR"
          },
          "deliveryCost": {
            "shippingCost": {
              "value": "4.95",
              "currency": "EUR"
            }
          },
          "appliedPromotions": [],
          "taxes": [],
          "properties": {
            "buyerProtection": true
          },
          "lineItemFulfillmentInstructions": {
            "minEstimatedDeliveryDate": "2026-09-23T22:00:00.000Z",
            "maxEstimatedDeliveryDate": "2026-09-24T22:00:00.000Z",
            "shipByDate": "2026-09-24T21:59:59.000Z",
            "guaranteedDelivery": false
          },
          "itemLocation": {
            "location": "Siek",
            "countryCode": "DE",
            "postalCode": "22962"
          }
        }
      ],
      "salesRecordReference": "58317",
      "totalFeeBasisAmount": {
        "value": "75.28",
        "currency": "EUR"
      },
      "totalMarketplaceFee": {
        "value": "12.19",
        "currency": "EUR"
      }
    },
    {
      "orderId": "12-15199-57289",
      "legacyOrderId": "12-15199-57289",
      "creationDate": "2026-09-23T08:06:35.000Z",
      "lastModifiedDate": "2026-09-23T09:41:52.000Z",
      "orderFulfillmentStatus": "FULFILLED",
      "orderPaymentStatus": "PAID",
      "sellerId": "handwerker3000_de",
      "buyer": {
        "username": "ml946",
        "taxAddress": {
          "city": "Nittenau",
          "postalCode": "93149",
          "countryCode": "DE"
        },
        "buyerRegistrationAddress": {
          "fullName": "Markus Luschberger",
          "contactAddress": {
            "addressLine1": "Kreuzweg 3",
            "city": "Nittenau",
            "postalCode": "93149",
            "countryCode": "DE"
          },
          "primaryPhone": {
            "phoneNumber": "9436902575"
          },
          "email": "008fcd504b1352154f6f@members.ebay.com"
        }
      },
      "pricingSummary": {
        "priceSubtotal": {
          "value": "264.8",
          "currency": "EUR"
        },
        "priceDiscount": {
          "value": "-13.24",
          "currency": "EUR"
        },
        "deliveryCost": {
          "value": "19.8",
          "currency": "EUR"
        },
        "deliveryDiscount": {
          "value": "-14.85",
          "currency": "EUR"
        },
        "total": {
          "value": "256.51",
          "currency": "EUR"
        }
      },
      "cancelStatus": {
        "cancelState": "NONE_REQUESTED",
        "cancelRequests": []
      },
      "paymentSummary": {
        "totalDueSeller": {
          "value": "216.28",
          "currency": "EUR"
        },
        "refunds": [],
        "payments": [
          {
            "paymentMethod": "EBAY",
            "paymentReferenceId": "420006_S",
            "paymentDate": "2026-09-23T08:06:36.033Z",
            "amount": {
              "value": "216.28",
              "currency": "EUR"
            },
            "paymentStatus": "PAID"
          }
        ]
      },
      "fulfillmentStartInstructions": [
        {
          "fulfillmentInstructionsType": "SHIP_TO",
          "minEstimatedDeliveryDate": "2026-09-23T22:00:00.000Z",
          "maxEstimatedDeliveryDate": "2026-09-24T22:00:00.000Z",
          "ebaySupportedFulfillment": false,
          "shippingStep": {
            "shipTo": {
              "fullName": "ML Bau & Energie GmbH",
              "contactAddress": {
                "addressLine1": "Kreuzweg 3",
                "city": "Nittenau",
                "postalCode": "93149",
                "countryCode": "DE"
              },
              "primaryPhone": {
                "phoneNumber": "09436902575"
              },
              "email": "008fcd504b1352154f6f@members.ebay.com"
            },
            "shippingCarrierCode": "DPD",
            "shippingServiceCode": "DE_DPDClassic"
          }
        }
      ],
      "fulfillmentHrefs": [
        "https://api.ebay.com/sell/fulfillment/v1/order/12-15199-57289/shipping_fulfillment/01235015953135",
        "https://api.ebay.com/sell/fulfillment/v1/order/12-15199-57289/shipping_fulfillment/01235015953134"
      ],
      "lineItems": [
        {
          "lineItemId": "10085146243212",
          "legacyItemId": "405522551284",
          "legacyVariationId": "675811813156",
          "variationAspects": [
            {
              "name": "Höhe",
              "value": "15mm (50 Stück)"
            }
          ],
          "sku": "15620.Kt",
          "title": "Nevoga Montageplatten / Unterlagsplatten 70x70mm voll Ausgleichplatten",
          "lineItemCost": {
            "value": "122.2",
            "currency": "EUR"
          },
          "discountedLineItemCost": {
            "value": "116.12",
            "currency": "EUR"
          },
          "quantity": 4,
          "soldFormat": "FIXED_PRICE",
          "listingMarketplaceId": "EBAY_DE",
          "purchaseMarketplaceId": "EBAY_DE",
          "lineItemFulfillmentStatus": "FULFILLED",
          "total": {
            "value": "121.07",
            "currency": "EUR"
          },
          "deliveryCost": {
            "shippingCost": {
              "value": "4.95",
              "currency": "EUR"
            },
            "discountAmount": {
              "value": "14.85",
              "currency": "EUR"
            }
          },
          "appliedPromotions": [
            {
              "discountAmount": {
                "value": "6.08",
                "currency": "EUR"
              },
              "promotionId": "15327109101",
              "description": "Sparen Sie bis zu 5% mit Multi-Rabatt"
            }
          ],
          "taxes": [],
          "properties": {
            "buyerProtection": true
          },
          "lineItemFulfillmentInstructions": {
            "minEstimatedDeliveryDate": "2026-09-23T22:00:00.000Z",
            "maxEstimatedDeliveryDate": "2026-09-24T22:00:00.000Z",
            "shipByDate": "2026-09-24T21:59:59.000Z",
            "guaranteedDelivery": false
          },
          "itemLocation": {
            "location": "Siek",
            "countryCode": "DE",
            "postalCode": "22962"
          }
        },
        {
          "lineItemId": "10085146243312",
          "legacyItemId": "405522551284",
          "legacyVariationId": "675811813155",
          "variationAspects": [
            {
              "name": "Höhe",
              "value": "10mm (125 Stück)"
            }
          ],
          "sku": "15619.Kt",
          "title": "Nevoga Montageplatten / Unterlagsplatten 70x70mm voll Ausgleichplatten",
          "lineItemCost": {
            "value": "142.6",
            "currency": "EUR"
          },
          "discountedLineItemCost": {
            "value": "135.44",
            "currency": "EUR"
          },
          "quantity": 4,
          "soldFormat": "FIXED_PRICE",
          "listingMarketplaceId": "EBAY_DE",
          "purchaseMarketplaceId": "EBAY_DE",
          "lineItemFulfillmentStatus": "FULFILLED",
          "total": {
            "value": "135.44",
            "currency": "EUR"
          },
          "deliveryCost": {
            "shippingCost": {
              "value": "0.0",
              "currency": "EUR"
            }
          },
          "appliedPromotions": [
            {
              "discountAmount": {
                "value": "7.16",
                "currency": "EUR"
              },
              "promotionId": "15327109101",
              "description": "Sparen Sie bis zu 5% mit Multi-Rabatt"
            }
          ],
          "taxes": [],
          "properties": {
            "buyerProtection": true
          },
          "lineItemFulfillmentInstructions": {
            "minEstimatedDeliveryDate": "2026-09-23T22:00:00.000Z",
            "maxEstimatedDeliveryDate": "2026-09-24T22:00:00.000Z",
            "shipByDate": "2026-09-24T21:59:59.000Z",
            "guaranteedDelivery": false
          },
          "itemLocation": {
            "location": "Siek",
            "countryCode": "DE",
            "postalCode": "22962"
          }
        }
      ],
      "salesRecordReference": "58316",
      "totalFeeBasisAmount": {
        "value": "256.51",
        "currency": "EUR"
      },
      "totalMarketplaceFee": {
        "value": "40.23",
        "currency": "EUR"
      }
    },
    {
      "orderId": "12-15199-41427",
      "legacyOrderId": "12-15199-41427",
      "creationDate": "2026-09-23T06:45:02.000Z",
      "lastModifiedDate": "2026-09-23T08:28:19.000Z",
      "orderFulfillmentStatus": "FULFILLED",
      "orderPaymentStatus": "PAID",
      "sellerId": "handwerker3000_de",
      "buyer": {
        "username": "motoradmossy21",
        "taxAddress": {
          "city": "Leichlingen",
          "postalCode": "42799",
          "countryCode": "DE"
        },
        "buyerRegistrationAddress": {
          "fullName": "Moritz natzke",
          "contactAddress": {
            "addressLine1": "oberbüscherhof 85",
            "city": "leichlingen",
            "postalCode": "42799",
            "countryCode": "DE"
          },
          "primaryPhone": {
            "phoneNumber": "1601434166"
          },
          "email": "11faea47285b22f5da57@members.ebay.com"
        }
      },
      "pricingSummary": {
        "priceSubtotal": {
          "value": "15.25",
          "currency": "EUR"
        },
        "deliveryCost": {
          "value": "4.95",
          "currency": "EUR"
        },
        "total": {
          "value": "20.2",
          "currency": "EUR"
        }
      },
      "cancelStatus": {
        "cancelState": "NONE_REQUESTED",
        "cancelRequests": []
      },
      "paymentSummary": {
        "totalDueSeller": {
          "value": "16.53",
          "currency": "EUR"
        },
        "refunds": [],
        "payments": [
          {
            "paymentMethod": "EBAY",
            "paymentReferenceId": "420004_S",
            "paymentDate": "2026-09-23T06:45:03.951Z",
            "amount": {
              "value": "16.53",
              "currency": "EUR"
            },
            "paymentStatus": "PAID"
          }
        ]
      },
      "fulfillmentStartInstructions": [
        {
          "fulfillmentInstructionsType": "SHIP_TO",
          "minEstimatedDeliveryDate": "2026-09-23T22:00:00.000Z",
          "maxEstimatedDeliveryDate": "2026-09-24T22:00:00.000Z",
          "ebaySupportedFulfillment": false,
          "shippingStep": {
            "shipTo": {
              "fullName": "Moritz Natzke",
              "contactAddress": {
                "addressLine1": "Oberbüscherhof 85",
                "city": "Leichlingen",
                "postalCode": "42799",
                "countryCode": "DE"
              },
              "primaryPhone": {
                "phoneNumber": "021743341"
              },
              "email": "11faea47285b22f5da57@members.ebay.com"
            },
            "shippingCarrierCode": "DPD",
            "shippingServiceCode": "DE_DPDClassic"
          }
        }
      ],
      "fulfillmentHrefs": [
        "https://api.ebay.com/sell/fulfillment/v1/order/12-15199-41427/shipping_fulfillment/01235015953126"
      ],
      "lineItems": [
        {
          "lineItemId": "10085145321712",
          "legacyItemId": "135651262421",
          "sku": "704.Ro",
          "title": "Baufolie transparent LDPE-Abdeckfolie 2 x 50 m (100 m²) Malerfokie, Abdeckplane",
          "lineItemCost": {
            "value": "15.25",
            "currency": "EUR"
          },
          "quantity": 1,
          "soldFormat": "FIXED_PRICE",
          "listingMarketplaceId": "EBAY_DE",
          "purchaseMarketplaceId": "EBAY_DE",
          "lineItemFulfillmentStatus": "FULFILLED",
          "total": {
            "value": "20.2",
            "currency": "EUR"
          },
          "deliveryCost": {
            "shippingCost": {
              "value": "4.95",
              "currency": "EUR"
            }
          },
          "appliedPromotions": [],
          "taxes": [],
          "properties": {
            "buyerProtection": true
          },
          "lineItemFulfillmentInstructions": {
            "minEstimatedDeliveryDate": "2026-09-23T22:00:00.000Z",
            "maxEstimatedDeliveryDate": "2026-09-24T22:00:00.000Z",
            "shipByDate": "2026-09-24T21:59:59.000Z",
            "guaranteedDelivery": false
          },
          "itemLocation": {
            "location": "Siek",
            "countryCode": "DE",
            "postalCode": "22962"
          }
        }
      ],
      "salesRecordReference": "58313",
      "totalFeeBasisAmount": {
        "value": "20.2",
        "currency": "EUR"
      },
      "totalMarketplaceFee": {
        "value": "3.67",
        "currency": "EUR"
      }
    },
    {
      "orderId": "12-15197-50083",
      "legacyOrderId": "12-15197-50083",
      "creationDate": "2026-09-22T19:13:51.000Z",
      "lastModifiedDate": "2026-09-23T05:15:35.000Z",
      "orderFulfillmentStatus": "FULFILLED",
      "orderPaymentStatus": "PAID",
      "sellerId": "handwerker3000_de",
      "buyer": {
        "username": "fx--audio",
        "taxAddress": {
          "city": "Freihung",
          "postalCode": "92271",
          "countryCode": "DE"
        },
        "buyerRegistrationAddress": {
          "fullName": "Fabian Klier",
          "contactAddress": {
            "addressLine1": "Im Dorf 5",
            "city": "Freihung",
            "postalCode": "92271",
            "countryCode": "DE"
          },
          "primaryPhone": {
            "phoneNumber": "964691294"
          },
          "email": "009254673978523622a5@members.ebay.com"
        }
      },
      "pricingSummary": {
        "priceSubtotal": {
          "value": "191.61",
          "currency": "EUR"
        },
        "deliveryCost": {
          "value": "0.0",
          "currency": "EUR"
        },
        "total": {
          "value": "191.61",
          "currency": "EUR"
        }
      },
      "cancelStatus": {
        "cancelState": "NONE_REQUESTED",
        "cancelRequests": []
      },
      "paymentSummary": {
        "totalDueSeller": {
          "value": "161.43",
          "currency": "EUR"
        },
        "refunds": [],
        "payments": [
          {
            "paymentMethod": "EBAY",
            "paymentReferenceId": "420004_S",
            "paymentDate": "2026-09-22T19:13:51.378Z",
            "amount": {
              "value": "161.43",
              "currency": "EUR"
            },
            "paymentStatus": "PAID"
          }
        ]
      },
      "fulfillmentStartInstructions": [
        {
          "fulfillmentInstructionsType": "SHIP_TO",
          "minEstimatedDeliveryDate": "2026-09-23T22:00:00.000Z",
          "maxEstimatedDeliveryDate": "2026-09-24T22:00:00.000Z",
          "ebaySupportedFulfillment": false,
          "shippingStep": {
            "shipTo": {
              "fullName": "Fabian   Klier",
              "contactAddress": {
                "addressLine1": "Im Dorf 5",
                "city": "Freihung",
                "postalCode": "92271",
                "countryCode": "DE"
              },
              "primaryPhone": {
                "phoneNumber": "01702131340"
              },
              "email": "009254673978523622a5@members.ebay.com"
            },
            "shippingCarrierCode": "DPD",
            "shippingServiceCode": "DE_DPDClassic"
          }
        }
      ],
      "fulfillmentHrefs": [
        "https://api.ebay.com/sell/fulfillment/v1/order/12-15197-50083/shipping_fulfillment/208568545431"
      ],
      "lineItems": [
        {
          "lineItemId": "10085134274712",
          "legacyItemId": "405524661118",
          "legacyVariationId": "675814720025",
          "variationAspects": [
            {
              "name": "Ausführung",
              "value": "3x8 - max. Arbeitshöhe 3,5-5,5m - 246x46x13cm-12kg"
            }
          ],
          "sku": "121871.St",
          "title": "Mehrzweckleiter 3-teilig aus Alu bis 150 kg belastbar, Stufenleiter, Leiter",
          "lineItemCost": {
            "value": "191.61",
            "currency": "EUR"
          },
          "quantity": 1,
          "soldFormat": "FIXED_PRICE",
          "listingMarketplaceId": "EBAY_DE",
          "purchaseMarketplaceId": "EBAY_DE",
          "lineItemFulfillmentStatus": "FULFILLED",
          "total": {
            "value": "191.61",
            "currency": "EUR"
          },
          "deliveryCost": {
            "shippingCost": {
              "value": "0.0",
              "currency": "EUR"
            }
          },
          "appliedPromotions": [],
          "taxes": [],
          "properties": {
            "buyerProtection": true
          },
          "lineItemFulfillmentInstructions": {
            "minEstimatedDeliveryDate": "2026-09-23T22:00:00.000Z",
            "maxEstimatedDeliveryDate": "2026-09-24T22:00:00.000Z",
            "shipByDate": "2026-09-23T21:59:59.000Z",
            "guaranteedDelivery": false
          },
          "itemLocation": {
            "location": "Siek",
            "countryCode": "DE",
            "postalCode": "22962"
          }
        }
      ],
      "salesRecordReference": "58312",
      "totalFeeBasisAmount": {
        "value": "191.61",
        "currency": "EUR"
      },
      "totalMarketplaceFee": {
        "value": "30.18",
        "currency": "EUR"
      }
    },
    {
      "orderId": "18-15185-76921",
      "legacyOrderId": "18-15185-76921",
      "creationDate": "2026-09-22T16:28:49.000Z",
      "lastModifiedDate": "2026-09-23T06:41:00.000Z",
      "orderFulfillmentStatus": "FULFILLED",
      "orderPaymentStatus": "PAID",
      "sellerId": "handwerker3000_de",
      "buyer": {
        "username": "aller1953",
        "taxAddress": {
          "city": "Kappelrodeck",
          "postalCode": "77876",
          "countryCode": "DE"
        },
        "buyerRegistrationAddress": {
          "fullName": "Alfred Steimle",
          "contactAddress": {
            "addressLine1": "Hauptstr. 182",
            "city": "Kappelrodeck",
            "postalCode": "77876",
            "countryCode": "DE"
          },
          "primaryPhone": {
            "phoneNumber": "16093093009"
          },
          "secondaryPhone": {
            "phoneNumber": "016093093009"
          },
          "email": "492998e590a90bd15774@members.ebay.com"
        }
      },
      "pricingSummary": {
        "priceSubtotal": {
          "value": "50.9",
          "currency": "EUR"
        },
        "priceDiscount": {
          "value": "-1.54",
          "currency": "EUR"
        },
        "deliveryCost": {
          "value": "9.9",
          "currency": "EUR"
        },
        "deliveryDiscount": {
          "value": "-4.95",
          "currency": "EUR"
        },
        "total": {
          "value": "54.31",
          "currency": "EUR"
        }
      },
      "cancelStatus": {
        "cancelState": "NONE_REQUESTED",
        "cancelRequests": []
      },
      "paymentSummary": {
        "totalDueSeller": {
          "value": "45.37",
          "currency": "EUR"
        },
        "refunds": [],
        "payments": [
          {
            "paymentMethod": "EBAY",
            "paymentReferenceId": "420004_S",
            "paymentDate": "2026-09-22T16:28:50.265Z",
            "amount": {
              "value": "45.37",
              "currency": "EUR"
            },
            "paymentStatus": "PAID"
          }
        ]
      },
      "fulfillmentStartInstructions": [
        {
          "fulfillmentInstructionsType": "SHIP_TO",
          "minEstimatedDeliveryDate": "2026-09-23T22:00:00.000Z",
          "maxEstimatedDeliveryDate": "2026-09-24T22:00:00.000Z",
          "ebaySupportedFulfillment": false,
          "shippingStep": {
            "shipTo": {
              "fullName": "Alfred Steimle",
              "contactAddress": {
                "addressLine1": "Hauptstr. 182",
                "city": "Kappelrodeck",
                "postalCode": "77876",
                "countryCode": "DE"
              },
              "primaryPhone": {
                "phoneNumber": "0784230591"
              },
              "email": "492998e590a90bd15774@members.ebay.com"
            },
            "shippingCarrierCode": "DPD",
            "shippingServiceCode": "DE_DPDClassic"
          }
        }
      ],
      "fulfillmentHrefs": [
        "https://api.ebay.com/sell/fulfillment/v1/order/18-15185-76921/shipping_fulfillment/01235015953106"
      ],
      "lineItems": [
        {
          "lineItemId": "10088067900118",
          "legacyItemId": "405632109470",
          "legacyVariationId": "675925877305",
          "variationAspects": [
            {
              "name": "Breite - VPE",
              "value": "300mm - 1 Rolle"
            }
          ],
          "sku": "121859.Ro",
          "title": "KIP 399 Express Mask selbstklebendes Abdeckpapier",
          "lineItemCost": {
            "value": "50.9",
            "currency": "EUR"
          },
          "discountedLineItemCost": {
            "value": "49.36",
            "currency": "EUR"
          },
          "quantity": 2,
          "soldFormat": "FIXED_PRICE",
          "listingMarketplaceId": "EBAY_DE",
          "purchaseMarketplaceId": "EBAY_DE",
          "lineItemFulfillmentStatus": "FULFILLED",
          "total": {
            "value": "54.31",
            "currency": "EUR"
          },
          "deliveryCost": {
            "shippingCost": {
              "value": "4.95",
              "currency": "EUR"
            },
            "discountAmount": {
              "value": "4.95",
              "currency": "EUR"
            }
          },
          "appliedPromotions": [
            {
              "discountAmount": {
                "value": "1.54",
                "currency": "EUR"
              },
              "promotionId": "15440535401",
              "description": "Sparen Sie bis zu 5% mit Multi-Rabatt"
            }
          ],
          "taxes": [],
          "properties": {
            "buyerProtection": true
          },
          "lineItemFulfillmentInstructions": {
            "minEstimatedDeliveryDate": "2026-09-23T22:00:00.000Z",
            "maxEstimatedDeliveryDate": "2026-09-24T22:00:00.000Z",
            "shipByDate": "2026-09-23T21:59:59.000Z",
            "guaranteedDelivery": false
          },
          "itemLocation": {
            "location": "Siek",
            "countryCode": "DE",
            "postalCode": "22962"
          }
        }
      ],
      "salesRecordReference": "58311",
      "totalFeeBasisAmount": {
        "value": "54.31",
        "currency": "EUR"
      },
      "totalMarketplaceFee": {
        "value": "8.94",
        "currency": "EUR"
      }
    }
  ]
}
```

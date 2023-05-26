using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

namespace ZudamalMavjiSomonServices
{
    static class Service
    {
        static private readonly ILog _log = LogManager.GetLogger(typeof(Service));
        static private readonly int _provId = -1; // NEED TO CHANGE

        static private readonly HashSet<int> _accept = new HashSet<int>() { };
        static private readonly HashSet<int> _cancel = new HashSet<int>() { };

        private enum StatusInDataBase : int
        {
            Awaiting = 0,
            Accept = 1,
            Cancel = 2
        }

        static private void TryToPay(Payment payment)
        {
            string response;
            string message = XmlDoc.CreateCardnoRequest(payment.Number);
            SocketConnect.Request(message);
            if (!SocketConnect.RequestPassed)
            {
                throw new MavjiSomonException("Socket request does not pass successfully");
            }

            response = SocketConnect.Response();

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(response);
            int rtnCode = int.Parse(xmlDocument.GetElementsByTagName("returncode")[0].InnerText);

            if(rtnCode != 0)
            {
                ModifyPaymentStatus(GetPaymentStatusDb(rtnCode), payment);
                throw new MavjiSomonException($"Return code: {rtnCode}");
            }

            message = XmlDoc.CreatePaymentRequest(payment.Number, payment.PaymentId, payment.ProvSum);
            SocketConnect.Request(message);

            if (!SocketConnect.RequestPassed)
            {
                throw new MavjiSomonException("Socket request does not pass successfully");
            }

            response = SocketConnect.Response();

            xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(response);
            rtnCode = int.Parse(xmlDocument.GetElementsByTagName("returncode")[0].InnerText);

            ModifyPaymentStatus(GetPaymentStatusDb(rtnCode), payment);
        }

        static public void Start()
        {
            GetPayments();
        }

        static public void GetPayments()
        {
            DataTable dt = SqlServer.GetData("GetPaymentsToSend", new SqlParameter[] { new SqlParameter("@ProviderID", _provId) });
            foreach (DataRow row in dt.Rows)
            {
                try
                {
                    Payment payment = new Payment();
                    if (decimal.Parse(row["ProviderSum"].ToString()) >= 15.0m)
                    {
                        payment.PaymentId = row["PaymentID"].ToString();
                        payment.Number = row["Number"].ToString();
                        payment.ProvSum = row["ProviderSum"].ToString();

                        TryToPay(payment);
                    }
                    else
                    {
                        ModifyPaymentStatus(StatusInDataBase.Cancel, payment);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }
            }
        }

        static private StatusInDataBase GetPaymentStatusDb(int code)
        {
            if (_accept.Contains(code))
            {
                return StatusInDataBase.Accept;
            }
            else if (_cancel.Contains(code))
            {
                return StatusInDataBase.Cancel;
            }

            return StatusInDataBase.Awaiting;
        }

        static private void ModifyPaymentStatus(StatusInDataBase status, Payment payment)
        {
            SqlServer.ExecSP("ModifyPaymentStatus", new SqlParameter[]
            {
                new SqlParameter("@PaymentID", payment.PaymentId),
                new SqlParameter("@ErrorCode", status),
                new SqlParameter("@ProviderPaymentID", payment.PaymentId),
                new SqlParameter("@ProviderID", _provId)
            });
        }
    }
}

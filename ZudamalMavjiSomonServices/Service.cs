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

        static private string _paymentId;
        static private string _number;
        static private string _provSum;

        private enum StatusInDataBase : int
        {
            Awaiting = 0,
            Accept = 1,
            Cancel = 2
        }

        static private void Initialize()
        {
            _paymentId = "";
            _number = "";
            _provSum = "";
        }

        static private void TryToPay()
        {
            //string cardno = "8121703244005555";
            string response;
            string message = XmlDoc.CreateCardnoRequest(_number);
            SocketConnect.Request(message);
            if (!SocketConnect.RequestPassed)
            {
                throw new MavjiSomonException();
            }

            response = SocketConnect.Response();

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(response);
            int rtnCode = int.Parse(xmlDocument.GetElementsByTagName("returncode")[0].InnerText);

            if(rtnCode != 0)
            {
                ModifyPaymentStatus(GetPaymentStatusDb(rtnCode));
                throw new MavjiSomonException($"return code: {rtnCode}");
            }

            message = XmlDoc.CreatePaymentRequest(_number, _paymentId, _provSum);
            SocketConnect.Request(message);

            if (!SocketConnect.RequestPassed)
            {
                throw new MavjiSomonException();
            }

            response = SocketConnect.Response();

            xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(response);
            rtnCode = int.Parse(xmlDocument.GetElementsByTagName("returncode")[0].InnerText);

            ModifyPaymentStatus(GetPaymentStatusDb(rtnCode));
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
                Initialize();
                if(decimal.Parse(row["ProviderSum"].ToString()) >= 15.0m)
                {
                    try
                    {
                        _paymentId = row["PaymentID"].ToString();
                        _number = row["Number"].ToString();
                        _provSum = row["ProviderSum"].ToString();

                        TryToPay();
                    }
                    catch(Exception ex)
                    {
                        _log.Error(ex);
                    }
                }
                else
                {
                    ModifyPaymentStatus(StatusInDataBase.Cancel);
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

        static private void ModifyPaymentStatus(StatusInDataBase status)
        {
            SqlServer.ExecSP("ModifyPaymentStatus", new SqlParameter[]
            {
                new SqlParameter("@PaymentID", _paymentId),
                new SqlParameter("@ErrorCode", status),
                new SqlParameter("@ProviderPaymentID", _paymentId),
                new SqlParameter("@ProviderID", _provId)
            });
        }
    }
}

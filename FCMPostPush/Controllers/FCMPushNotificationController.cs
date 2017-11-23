using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using System.Web.WebSockets;
using FCMPostPush.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FCMPostPush.Controllers
{
    public class FCMPushNotificationController : ApiController
    {
        public AppRequestModels PostAppData(AppRequestModels obj)
        {
            AppRequestModels armReturn = new AppRequestModels();

            armReturn.RegistrationID = obj.RegistrationID;

            return armReturn;
        }

        public FCMPushMessage PushMessage(FCMPushMessage obj)
        {
            FCMPushMessage fpmReturn = new FCMPushMessage();

            fpmReturn.APIKey = obj.APIKey;
            fpmReturn.RegID = obj.RegID;
            fpmReturn.Message = obj.Message;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://android.googleapis.com/gcm/send");
            request.Method = "POST";
            request.ContentType = "application/json;charset=utf-8;";
            request.Headers.Add($"Authorization: key={fpmReturn.APIKey}");

            string RegistrationID = fpmReturn.RegID;
            var postData =
            new
            {
                data = new
                {
                    message = fpmReturn.Message //message這個tag要讓前端開發人員知道
                        },
                registration_ids = new string[] { RegistrationID }
            };
            string p = JsonConvert.SerializeObject(postData);//將Linq to json轉為字串
            byte[] byteArray = Encoding.UTF8.GetBytes(p);//要發送的字串轉為byte[]
            request.ContentLength = byteArray.Length;

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            //發出Request
            WebResponse response = request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            string responseStr = reader.ReadToEnd();
            reader.Close();
            responseStream.Close();
            response.Close();

            JObject oJSON = (JObject)JsonConvert.DeserializeObject(responseStr);
            if (Convert.ToInt32(oJSON["failure"].ToString()) > 0)
            {//有失敗情況就寫Log
                //EventLog.WriteEntry("發送訊息給" + RegistrationID + "失敗：" + responseStr);

                oJSON = (JObject)oJSON["results"][0];
                if (oJSON["error"].ToString() == "InvalidRegistration" || oJSON["error"].ToString() == "NotRegistered")
                { //無效的RegistrationID
                  //從DB移除
                    SqlParameter[] param = new SqlParameter[] { new SqlParameter() { ParameterName = "@RegistrationID", SqlDbType = SqlDbType.VarChar, Value = RegistrationID } };
                    //SqlHelper.ExecteNonQuery(CommandType.Text, "Delete from tb_MyRegisID Where RegistrationID=@RegistrationID", param);

                }
            }
            //returnStr.Append(responseStr + "\n");

            return fpmReturn;    
        }

        // GET: FCMPushNotification
        public ActionResult Index()
        {
            return View();
        }
    }
}
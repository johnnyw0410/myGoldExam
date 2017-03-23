using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronWebSocketClient;
using Crestron.SimplSharp.Net.Https;
using Crestron.SimplSharp.Net;
using Newtonsoft.Json;

namespace PushBulletClassV1._00
{
    abstract class blah{}
    
    public class PBClass
    {
        public static string User_Token;
        public string User_Email;
        public string PushReceived;
        public string PrintString;
        public string PrintString2;
        public bool WSSReceivedPush = false;
        private bool flag = false;
        public WebSocketClient MyWSConnection = new WebSocketClient();
        WebSocketClient.WEBSOCKET_RESULT_CODES MyWSConnectionResult;

        public PBClass() { }
   
        //Event Handler
        public event WSSReceiveHandler onReceivePush;
        public delegate void WSSReceiveHandler(object o, EventArgs e);

        //Websocket Callback functions
        public int SendCallback(WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            try
            {
                MyWSConnection.ReceiveAsync();
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("SendCallback Error" + e.ToString());
                return -1;
            }
            return 0;
        }

        public int ReceiveCallback(byte[] data, uint datalen, WebSocketClient.WEBSOCKET_PACKET_TYPES opcode, /*JW MAC*/ WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            string sData = Encoding.ASCII.GetString(data, 0, data.Length);

            try
            {
                socketDataType msg = JsonConvert.DeserializeObject<socketDataType>(sData);
                CrestronConsole.PrintLine("Received data: " + sData.ToString());
                
                if (msg.type == "nop")
                {
                    WSSReceivedPush = false;
                    flag = false;
                }
                if (msg.type == "tickle")
                {
                    WSSReceivedPush = true;
                    if (onReceivePush != null && flag == false)
                    {
                        onReceivePush(null, null);
                        flag = true;
                    }
                    else if (onReceivePush != null && flag == true)
                    {
                        flag = false;
                    }
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine("Unknown Data Received" + e.ToString());
                Disconnect();
            }
            MyWSConnection.ReceiveAsync();
            return 0;
        }

        //socketDataType class
        public class socketDataType
        {
            public string type, subtype;
        }
        
        //SetToken method for setting the user token
        public void SetToken(string UserToken)
        {
            User_Token = UserToken;
            PrintString = "User Token Has Been Set to: " + User_Token;
        }

        //Connect Method for establishing the WebSocket Connection
        public void Connect()
        {
            MyWSConnection.Port = 443;
            MyWSConnection.SSL = true;
            MyWSConnection.URL = "wss://stream.pushbullet.com/websocket/" + User_Token;
            MyWSConnection.ConnectionCallBack = SendCallback;
            MyWSConnection.ReceiveCallBack = ReceiveCallback;

            MyWSConnectionResult = MyWSConnection.Connect();

            if (MyWSConnectionResult == (int)WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
            {
                PrintString = "Connection Success!";

                PrintString2 = "This is the msg: " + MyWSConnection.ReceiveAsync();
            }
            else
            {
                PrintString = "Cannot Establish Web Socket Connection. Err Code:" + MyWSConnectionResult.ToString();
            }        
        }

        //Disconnect method for disconnecting the WebSocket Connection
        public void Disconnect()
        {
            MyWSConnection.Disconnect();
            PrintString = "Web Socket Disconnected";
        }

        //GetUserEmail function
        public void GetUserEmail()
        {
            if (User_Token != "")
            {
                HttpsClient MyHttpsConnection = new HttpsClient();
                HttpsClientRequest MyHttpsRequest = new HttpsClientRequest();
                HttpsClientResponse MyHttpsResponse;
                string HttpsUrl;
                string MyTempString;
                string[] words;
                string commandstring = "";
                bool FoundEmail = false;

                MyHttpsConnection.PeerVerification = false;
                MyHttpsConnection.HostVerification = false;
                MyHttpsConnection.Verbose = false;
                HttpsUrl = "https://api.pushbullet.com/v2/users/me";

                MyHttpsRequest.KeepAlive = true;
                MyHttpsRequest.Url.Parse(HttpsUrl);
                MyHttpsRequest.RequestType = Crestron.SimplSharp.Net.Https.RequestType.Get;
                MyHttpsRequest.Header.SetHeaderValue("Authorization", "Bearer " + User_Token);

                PrintString2 = User_Token;

                MyHttpsRequest.ContentString = commandstring;

                // Dispatch will actually make the request with the server
                MyHttpsResponse = MyHttpsConnection.Dispatch(MyHttpsRequest);
                MyHttpsConnection.Abort();

                MyTempString = MyHttpsResponse.ContentString.ToString();
                words = MyTempString.Split(',');

                PrintString = "";

                foreach (string word in words)
                {
                    PrintString = PrintString + '+' + word;

                    if (word.Contains("email_normalized"))
                    {

                        User_Email = word.Substring(20, word.Length - 21);

                        FoundEmail = true;
                    }
                }
                if (!FoundEmail)
                {
                    User_Email = "Email Not Found!";
                }
            }
            else
                PrintString = "No Token Found";
        }

        //GetPush method that establish the https connection with PushBullet API and retrieves the Pushes
        public void GetPush()
        {
            if (User_Token != "")
            {
                HttpsClient MyHttpsConnection = new HttpsClient();
                HttpsClientRequest MyHttpsRequest = new HttpsClientRequest();
                HttpsClientResponse MyHttpsResponse;
                string HttpsUrl;
                string MyTempString;
                string[] words;
                string commandstring = "";
                bool FoundBody = false;

                MyHttpsConnection.PeerVerification = false;
                MyHttpsConnection.HostVerification = false;
                MyHttpsConnection.Verbose = false;
                MyHttpsConnection.UserName = User_Token;
                //limit the push outputs to 3
                HttpsUrl = "https://api.pushbullet.com/v2/pushes?limit=3";

                MyHttpsRequest.KeepAlive = true;
                MyHttpsRequest.Url.Parse(HttpsUrl);
                MyHttpsRequest.RequestType = Crestron.SimplSharp.Net.Https.RequestType.Get;
                MyHttpsRequest.Header.SetHeaderValue("Content-Type", "application/json");
                MyHttpsRequest.Header.SetHeaderValue("Authorization", "Bearer " + User_Token);

                PrintString2 = User_Token;

                MyHttpsRequest.ContentString = commandstring;

                // Dispatch will actually make the request with the server
                MyHttpsResponse = MyHttpsConnection.Dispatch(MyHttpsRequest);
                MyHttpsConnection.Abort();

                MyTempString = MyHttpsResponse.ContentString.ToString();
                words = MyTempString.Split(',');

                PrintString = "";

                foreach (string word in words)
                {
                    PrintString = PrintString + '+' + word;

                    if (word.Contains("body"))
                    {
                        PushReceived = word.Substring(8, word.Length - 10);

                        PushReceived = PushReceived.ToLower();

                        FoundBody = true;

                        break;
                    }
                }

                if (!FoundBody)
                {
                    User_Email = "Push Message Not Received";
                }
            }
            else
                PrintString = "No Token Found";
        }

        //Message class for Message getter and setter 
        public class Message
        {
            public string type { get; set; }
            public string title { get; set; }
            public string body { get; set; }
            public string email { get; set; }
        }

        //createNote method creates a json string that type = "note"
        public string createNote(string email, string title, string body)
        {
            string jsonString = "";

            if (User_Token != "")
            {
                Message msg = new Message
                {
                    type = "note",
                    title = title,
                    body = body,
                    email = email
                };

                jsonString = JsonConvert.SerializeObject(msg, Formatting.Indented);
            }
            return jsonString;
        }
        
        //SendPush method that send a push message to the PushBullet Server
        public void SendPush(string message)
        {
            HttpsClient client = new HttpsClient();
            client.PeerVerification = false;
            client.HostVerification = false;
            client.Verbose = false;

            HttpsClientRequest request = new HttpsClientRequest();
            HttpsClientResponse response;
            String url = "https://api.pushbullet.com/v2/pushes";

            request.KeepAlive = true;
            request.Url.Parse(url);
            client.UserName = User_Token;
            request.RequestType = Crestron.SimplSharp.Net.Https.RequestType.Post;
            request.Header.SetHeaderValue("Content-Type", "application/json");
            request.Header.SetHeaderValue("Authorization", "Bearer " + User_Token);
            request.ContentString = message;
            response = client.Dispatch(request);
        }

    }
}

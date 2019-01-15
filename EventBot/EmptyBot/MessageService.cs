using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EventBot
{
    public class MessageService
    {
        public static string GetMessage(string type)
        {
            using (StreamReader r = new StreamReader("messages.json"))
            {
                string json = r.ReadToEnd();
                dynamic jsonObject = JsonConvert.DeserializeObject(json);
                List<string> messages = new List<string>();
                foreach (var message in jsonObject[type])
                {
                    messages.Add(message.Value);
                }
                int length = messages.Count;
                Random rnd = new Random();
                int i = rnd.Next(0, length);
                return messages[i];
            }
        }
    }
}

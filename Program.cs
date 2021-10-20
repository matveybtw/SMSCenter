using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using MenuNamespace;
using SMSC;
namespace SMSC
{
    public interface IMessage
    {
        string Message { get; set; }
        string Phone { get; set; }
        string Id { get; set; }
    }
    public class SMSMessage : IMessage
    {
        string Text;
        string phone;
        string id;
        public string Message { get => Text; set => Text = value; }
        public string Phone { get => phone; set => phone = value; }
        public string Id { get => id; set => id = value; }
    }
    public class ViberMessage : IMessage
    {
        string Text;
        string phone;
        string id;
        public string Message { get => Text; set => Text = value; }
        public string Phone { get => phone; set => phone = value; }
        public string Id { get => id; set => id = value; }
    }
    public enum MessageStatus
    {
        NotFound = -3,
        Stopped,
        WaitingForDispatch,
        TransferredToOperator,
        Delivered,
        Readed,
        Stitched,
        ClickedLink,
        UnableToDeliver = 20,
        WrongNumber = 22,
        Forbidden,
        InsufficientFunds,
        UnavailableNumber
    }
    public class SMSCenter
    {

        private class SMSC
        {
            string SMSC_LOGIN = "matveybtw";
            string SMSC_PASSWORD = "uhLdCq5W";
            bool SMSC_POST = false;
            const bool SMSC_HTTPS = false;
            const string SMSC_CHARSET = "utf-8";
            const bool SMSC_DEBUG = false;
            public string[][] D2Res;
            public SMSC(string l, string p)
            {
                SMSC_LOGIN = l;
                SMSC_PASSWORD = p;
            }
            public string[] send_sms(string phones, string message, int translit = 0, string time = "", int id = 0, int format = 0, string sender = "", string query = "", string[] files = null)
            {
                if (files != null)
                    SMSC_POST = true;

                string[] formats = { "flash=1", "push=1", "hlr=1", "bin=1", "bin=2", "ping=1", "mms=1", "mail=1", "call=1", "viber=1", "soc=1" };

                string[] m = _smsc_send_cmd("send", "cost=3&phones=" + _urlencode(phones)
                                + "&mes=" + _urlencode(message) + "&id=" + id.ToString() + "&translit=" + translit.ToString()
                                + (format > 0 ? "&" + formats[format - 1] : "") + (sender != "" ? "&sender=" + _urlencode(sender) : "")
                                + (time != "" ? "&time=" + _urlencode(time) : "") + (query != "" ? "&" + query : ""), files);

                if (SMSC_DEBUG)
                {
                    if (Convert.ToInt32(m[1]) > 0)
                        _print_debug("Сообщение отправлено успешно. ID: " + m[0] + ", всего SMS: " + m[1] + ", стоимость: " + m[2] + ", баланс: " + m[3]);
                    else
                        _print_debug("Ошибка №" + m[1].Substring(1, 1) + (m[0] != "0" ? ", ID: " + m[0] : ""));
                }

                return m;
            }
            public string[] get_sms_cost(string phones, string message, int translit = 0, int format = 0, string sender = "", string query = "")
            {
                string[] formats = { "flash=1", "push=1", "hlr=1", "bin=1", "bin=2", "ping=1", "mms=1", "mail=1", "call=1", "viber=1", "soc=1" };

                string[] m = _smsc_send_cmd("send", "cost=1&phones=" + _urlencode(phones)
                                + "&mes=" + _urlencode(message) + translit.ToString() + (format > 0 ? "&" + formats[format - 1] : "")
                                + (sender != "" ? "&sender=" + _urlencode(sender) : "") + (query != "" ? "&query" : ""));
                if (SMSC_DEBUG)
                {
                    if (Convert.ToInt32(m[1]) > 0)
                        _print_debug("Стоимость рассылки: " + m[0] + ". Всего SMS: " + m[1]);
                    else
                        _print_debug("Ошибка №" + m[1].Substring(1, 1));
                }

                return m;
            }
            public string[] get_status(string id, string phone, int all = 0)
            {
                string[] m = _smsc_send_cmd("status", "phone=" + _urlencode(phone) + "&id=" + _urlencode(id) + "&all=" + all.ToString());
                if (id.IndexOf(',') == -1)
                {
                    if (SMSC_DEBUG)
                    {
                        if (m[1] != "" && Convert.ToInt32(m[1]) >= 0)
                        {
                            int timestamp = Convert.ToInt32(m[1]);
                            DateTime offset = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            DateTime date = offset.AddSeconds(timestamp);

                            _print_debug("Статус SMS = " + m[0] + (timestamp > 0 ? ", время изменения статуса - " + date.ToLocalTime() : ""));
                        }
                        else
                            _print_debug("Ошибка №" + m[1].Substring(1, 1));
                    }

                    int idx = all == 1 ? 9 : 12;

                    if (all > 0 && m.Length > idx && (m.Length < idx + 5 || m[idx + 5] != "HLR"))
                        m = String.Join(",", m).Split(",".ToCharArray(), idx);
                }
                else
                {
                    if (m.Length == 1 && m[0].IndexOf('-') == 2)
                        return m[0].Split(',');

                    Array.Resize(ref D2Res, 0);
                    Array.Resize(ref D2Res, m.Length);

                    for (int i = 0; i < D2Res.Length; i++)
                        D2Res[i] = m[i].Split(',');

                    Array.Resize(ref m, 1);
                    m[0] = "1";
                }

                return m;
            }
            public string get_balance()
            {
                string[] m = _smsc_send_cmd("balance", ""); // (balance) или (0, -error)

                if (SMSC_DEBUG)
                {
                    if (m.Length == 1)
                        _print_debug("Сумма на счете: " + m[0]);
                    else
                        _print_debug("Ошибка №" + m[1].Substring(1, 1));
                }

                return m.Length == 1 ? m[0] : "";
            }
            private string[] _smsc_send_cmd(string cmd, string arg, string[] files = null)
            {
                string url, _url;

                arg = "login=" + _urlencode(SMSC_LOGIN) + "&psw=" + _urlencode(SMSC_PASSWORD) + "&fmt=1&charset=" + SMSC_CHARSET + "&" + arg;

                url = _url = (SMSC_HTTPS ? "https" : "http") + "://smsc.ua/sys/" + cmd + ".php" + (SMSC_POST ? "" : "?" + arg);

                string ret;
                int i = 0;
                HttpWebRequest request;
                StreamReader sr;
                HttpWebResponse response;
                do
                {
                    if (i++ > 0)
                        url = _url.Replace("smsc.ua/", "www" + i.ToString() + ".smsc.ua/");

                    request = (HttpWebRequest)WebRequest.Create(url);

                    if (SMSC_POST)
                    {
                        request.Method = "POST";

                        string postHeader, boundary = "----------" + DateTime.Now.Ticks.ToString("x");
                        byte[] postHeaderBytes, boundaryBytes = Encoding.ASCII.GetBytes("--" + boundary + "--\r\n"), tbuf;
                        StringBuilder sb = new StringBuilder();
                        int bytesRead;

                        byte[] output = new byte[0];

                        if (files == null)
                        {
                            request.ContentType = "application/x-www-form-urlencoded";
                            output = Encoding.UTF8.GetBytes(arg);
                            request.ContentLength = output.Length;
                        }
                        else
                        {
                            request.ContentType = "multipart/form-data; boundary=" + boundary;

                            string[] par = arg.Split('&');
                            int fl = files.Length;
                            for (int pcnt = 0; pcnt < par.Length + fl; pcnt++)
                            {
                                sb.Clear();

                                sb.Append("--");
                                sb.Append(boundary);
                                sb.Append("\r\n");
                                sb.Append("Content-Disposition: form-data; name=\"");
                                bool pof = pcnt < fl;
                                String[] nv = new String[0];
                                if (pof)
                                {
                                    sb.Append("File" + (pcnt + 1));
                                    sb.Append("\"; filename=\"");
                                    sb.Append(Path.GetFileName(files[pcnt]));
                                }
                                else
                                {
                                    nv = par[pcnt - fl].Split('=');
                                    sb.Append(nv[0]);
                                }
                                sb.Append("\"");
                                sb.Append("\r\n");
                                sb.Append("Content-Type: ");
                                sb.Append(pof ? "application/octet-stream" : "text/plain; charset=\"" + SMSC_CHARSET + "\"");
                                sb.Append("\r\n");
                                sb.Append("Content-Transfer-Encoding: binary");
                                sb.Append("\r\n");
                                sb.Append("\r\n");
                                postHeader = sb.ToString();
                                postHeaderBytes = Encoding.UTF8.GetBytes(postHeader);
                                output = _concatb(output, postHeaderBytes);
                                if (pof)
                                {
                                    FileStream fileStream = new FileStream(files[pcnt], FileMode.Open, FileAccess.Read);

                                    // Write out the file contents
                                    byte[] buffer = new Byte[checked((uint)Math.Min(4096, (int)fileStream.Length))];

                                    bytesRead = 0;
                                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                                    {
                                        tbuf = buffer;
                                        Array.Resize(ref tbuf, bytesRead);

                                        output = _concatb(output, tbuf);
                                    }
                                }
                                else
                                {
                                    byte[] vl = Encoding.UTF8.GetBytes(nv[1]);
                                    output = _concatb(output, vl);
                                }

                                output = _concatb(output, Encoding.UTF8.GetBytes("\r\n"));
                            }
                            output = _concatb(output, boundaryBytes);

                            request.ContentLength = output.Length;
                        }
                        Stream requestStream = request.GetRequestStream();
                        requestStream.Write(output, 0, output.Length);
                    }
                    try
                    {
                        response = (HttpWebResponse)request.GetResponse();

                        sr = new StreamReader(response.GetResponseStream());
                        ret = sr.ReadToEnd();
                    }
                    catch (WebException)
                    {
                        ret = "";
                    }
                }
                while (ret == "" && i < 5);

                if (ret == "")
                {
                    if (SMSC_DEBUG)
                        _print_debug("Ошибка чтения адреса: " + url);

                    ret = ",";
                }

                char delim = ',';

                if (cmd == "status")
                {
                    string[] par = arg.Split('&');

                    for (i = 0; i < par.Length; i++)
                    {
                        string[] lr = par[i].Split("=".ToCharArray(), 2);

                        if (lr[0] == "id" && lr[1].IndexOf("%2c") > 0)
                            delim = '\n';
                    }
                }

                return ret.Split(delim);
            }
            private string _urlencode(string str)
            {
                if (SMSC_POST) return str;

                return HttpUtility.UrlEncode(str);
            }
            private byte[] _concatb(byte[] farr, byte[] sarr)
            {
                int opl = farr.Length;

                Array.Resize(ref farr, farr.Length + sarr.Length);
                Array.Copy(sarr, 0, farr, opl, sarr.Length);

                return farr;
            }
            private void _print_debug(string str)
            {
                Console.WriteLine("\nDebug\n" + str);
            }
        }
        private SMSC smsc;
        public SMSCenter(string login, string pass)
        {
            smsc = new SMSC(login, pass);
        }
        public void SendSms(SMSMessage message)
        {
            message.Id = smsc.send_sms(message.Phone, message.Message)[0];
        }
        public void SendViber(ViberMessage message)
        {
            message.Id = smsc.send_sms(message.Phone, message.Message, format: 10)[0];
        }
        public MessageStatus GetStatus(IMessage message)
        {
            return (MessageStatus)int.Parse(smsc.get_status(message.Id, message.Phone)[0]);
        }
        public decimal GetBalance()
        {
            var b = smsc.get_balance().Replace('.', ',');
            return Convert.ToDecimal(b);
        }
    }

}
namespace ProjectSMS
{
    class Program
    {
        static void Pause()
        {
            Console.Write("Для продолжения нажмите любую клавишу...");
            Console.ReadKey(false);
        }
        static void Main(string[] args)
        {
            SMSCenter smsc =null;
            Menu menu = new Menu(
                new List<string>()
                {
                    "Войти",
                    "Ваш баланс",
                    "Отправить смс",
                    "Отправить вайбер сообщение"
                });
            bool end=false;
            int value;
            bool authorized=false;
            string phonepatt = @"^\+380\d{9}";
            string ph;
            IMessage mssg=null;
            do
            {
                value = menu.StartMenu();
                Console.Clear();
                switch (value)
                {
                    case 0:
                        Console.Write("Введите логин: ");
                        string l = Console.ReadLine();
                        Console.Write("Введите пароль: ");
                        string p = Console.ReadLine();
                        smsc = new SMSCenter(l,p);
                        authorized = true;
                        break;
                    case 1:
                        if (!authorized)
                        {
                            Console.WriteLine("Вы не авторизированы.");
                            break;
                        }
                        Console.WriteLine("Ваш баланс: "+smsc.GetBalance());
                        break;
                    case 2:
                        if (!authorized)
                        {
                            Console.WriteLine("Вы не авторизированы.");
                            break;
                        }
                        Console.Write("Введите сообщение: ");
                        string m = Console.ReadLine();
                        ReadS:
                        Console.Write("Введите номер получателя(в формате +380XXXXXXXXX): ");
                        ph = Console.ReadLine();
                        if (!Regex.IsMatch(ph,phonepatt))
                        {
                            Console.WriteLine("Неверный формат");
                            goto ReadS;
                        }
                        mssg = new SMSMessage() { Message = m, Phone = ph };
                        smsc.SendSms(mssg as SMSMessage);
                        Console.WriteLine("Статус сообщения: {0}.", smsc.GetStatus(mssg).ToString());
                        break;
                    case 3:
                        if (!authorized)
                        {
                            Console.WriteLine("Вы не авторизированы.");
                            break;
                        }
                        Console.Write("Введите сообщение: ");
                        string me = Console.ReadLine();
                        ReadV:
                        Console.Write("Введите номер получателя: ");
                        ph = Console.ReadLine();
                        if (!Regex.IsMatch(ph, phonepatt))
                        {
                            Console.WriteLine("Неверный формат");
                            goto ReadV;
                        }
                        mssg = new ViberMessage() { Message = me, Phone = ph };
                        smsc.SendViber(mssg as ViberMessage);
                        Console.WriteLine("Статус сообщения: {0}.", smsc.GetStatus(mssg).ToString());
                        break;
                    case -1:
                        end = true;
                        break;
                }
                Pause();
                
            } while (!end);
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class Form1 : Form
    {
        public string path = string.Empty;
        public const int port = 8808;
        public delegate void InvokeDelegate(string s); // Duhet për të aksesuar elementët e gui-it në një thread tjetër. Sipas dokumentacionit zyrtar.
        public string emratEDosjeve = string.Empty;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Console.WriteLine(fbd.SelectedPath.ToString());
                path = fbd.SelectedPath.ToString();
                emratEDosjeve = getFilesNames(path); // Marrim nje string ku emrat e filave ndahen nga hapsira
                Console.WriteLine("Pathi eshte: " + path);
                button1.Hide();
                label1.Text = "Serveri Filloi ";
                setupServer();
            }
        }

        private void setupServer()
        {

            TcpListener serverSocket = new TcpListener(IPAddress.Any,port);
            //TcpClient clientSocket = default(TcpClient);
            int counter = 0;

            serverSocket.Start();
            Console.WriteLine(" >> " + "Server Started");
            new Thread(()=> {
                counter = 0;
                while (true)
                {
                    counter += 1;
                    TcpClient clientSocket = serverSocket.AcceptTcpClient(); //Pret deri sa te vij nje klient dhe proçedon
                    NetworkStream stream = clientSocket.GetStream();
                    Console.WriteLine(" >> " + "Client No:" + Convert.ToString(counter) + " started!");
                    new Thread(() =>
                    {
                        while (true)
                        {
                            if (stream.DataAvailable)//Shikon nëse ka të dhëna nga klienti
                            {
                                byte[] receivedBytes = ReadToEnd(stream);
                                string message = Encoding.UTF8.GetString(receivedBytes);

                                llogjikoMesazhin(message, stream);
                                label1.BeginInvoke(new InvokeDelegate(InvokeMethod), message);
                            }
                            else Thread.Sleep(1); //Alternative Duhet një funksion "is-alive" -Kod i keq i pashkallëzueshëm
                        }
                    }).Start();
                  
                    /*handleClinet client = new handleClinet();
                    client.startClient(clientSocket, Convert.ToString(counter));*/
                }
            }).Start();
           
        }

        private byte[] ReadToEnd(NetworkStream stream)
        {
            List<Byte> recivedBytes = new List<byte>();

            while (stream.DataAvailable)
            {

                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                byte[] correctBuffer = new byte[bytesRead];
                Array.Copy(buffer, correctBuffer, bytesRead);

                recivedBytes.AddRange(correctBuffer);//Shtohet ne fund te listes
            }

            // recivedBytes.RemoveAll(b => b == 0); //Njëri nga mesazhet nuk do te përmbushi dot 1024 => pjesa e pa përmbushur do të hiqet

            return recivedBytes.ToArray();
        }
    
        public void write(NetworkStream stream,byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }

        public void InvokeMethod(String message) //Funksioni që thërritet is lamda
        {
            label1.Text = message;
        }

        public string getFilesNames(string path)
        {
            string a = string.Empty;
            

            try
            {
                foreach (string f in Directory.GetFiles(path))
                {
                    //Kod i keq
                    string d = f.Replace(path + "\\", ""); //Kodi më lart kthen të gjithë pathin e filave jo vetëm emrin, duhet te ekzistoj një gjë qå kthen vetëm emrin
                    a += d + " ";
                }
                Console.WriteLine(a);
            }
            catch (System.Exception excpt)
            {
                MessageBox.Show(excpt.Message);
            }

            return a;
        }

        private void llogjikoMesazhin(string message,NetworkStream stream)
        {
            //Jep
            if (message.Equals("më jep listën"))
            {
                write(stream,Encoding.UTF8.GetBytes(emratEDosjeve));
            }
            else
            {
                //TO DO  Dergo file-in në byte.
                string vendiDosjes = path + "\\" + message;
                byte[] bytes = File.ReadAllBytes(vendiDosjes); //E converton dosjen në byte
                write(stream, bytes); //E dergon dosjen klientit
            }
        }
    }
}

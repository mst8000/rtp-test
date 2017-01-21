using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace AudioExchangerCsConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //モード選択
            Console.WriteLine("モード（0=通話待受，1=通話開始）: ");
            string userInputString = Console.ReadLine();
            if (userInputString == "0")
            {

            }
            else if(userInputString == "1")
            {

            }
            else
            {
                
            }

        }



    }
}

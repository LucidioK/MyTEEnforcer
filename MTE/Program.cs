using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTE
{
    static class Program
    {
        static void Main()
        {
            if (!Directory.Exists(@"c:\temp"))
            {
                Directory.CreateDirectory(@"c:\temp");
            }
            var enforcer = new MyTeEnforcer();
            while (true)
            {
                try
                {
                    System.Console.WriteLine($"MTE {DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}");
                    if (enforcer.MustEnforceToday())
                    {
                        do
                        {
                            enforcer.OpenMyTe();
#if DEBUG
                    System.Threading.Thread.Sleep(5 * 1000);
#else
                            System.Threading.Thread.Sleep(2 * 60 * 1000);
#endif
                        }
                        while (!enforcer.WasSubmitted());
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(120 * 60 * 1000);
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine($"MTE {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }
}


using System;


namespace Hello
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Your Name:");
            String name = Console.ReadLine();
            
            Console.WriteLine("How Many hours of sleep did you get last night?");
            int hoursOfSleep = int.Parse( Console.ReadLine() );                 //parse converts string into integer

            Console.WriteLine("hello, " + name);
            if (hoursOfSleep > 8){
                Console.WriteLine("you are well rested.");
            }
            else
            {
                Console.WriteLine("you need more sleep.");
            }
           
        }
    }
}

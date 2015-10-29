using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grades
{
    class Program
    {
        static void Main(string[] args)
        {
            GradeBook book = new GradeBook();  //invoke the constructor to make a new Gradebook object
            book.AddGrade(91);
            book.AddGrade(89.5f);               // apply an 'f' to show that it's a floating point number
        }
    }
}

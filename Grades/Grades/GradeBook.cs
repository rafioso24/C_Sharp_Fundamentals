using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grades
{
    class GradeBook{ //class = the cookie cutter

        public GradeBook() 
        {
        }
        public void AddGrade(float grade){
            grades.Add(grade);
            
        }
        List<float> grades = new List<float>(); //imediately initialize this field to a new list
    }
}

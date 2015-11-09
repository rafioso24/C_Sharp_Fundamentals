using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grades
{
   public class GradeBook{ //class = the cookie cutter

        public GradeBook() 
        {
        }
        public GradeStatistics ComputeStatistics()
        {
            GradeStatistics stats =  new GradeStatistics();
            float sum = 0;
            foreach (float grade in grades)
            {
                stats.HighestGrade = Math.Max(grade, stats.HighestGrade);
                stats.LowestGrade = Math.Min(grade, stats.LowestGrade);
                sum += grade;
            }
            stats.AverageGrade = sum / grades.Count;

            return stats;
        }
        public void AddGrade(float grade){
            grades.Add(grade);
            
        }
        private List<float> grades = new List<float>(); //imediately initialize this field to a new list
    }
}

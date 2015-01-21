using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CourseCenter.Models
{
    public class TcComment
    {
        public int Id { get; set; }
        public int EvTableId { get; set; }//评价量表ID
        public string TcAnswer { get; set; }//教师评价结果
        public int MouduleTag { get; set; }//模块Id
        public int CourseId { get; set; }
        public Guid TeacherId { get; set; }
        public Guid StudentId { get; set; }
        public double TScore { get; set; }
    }
}
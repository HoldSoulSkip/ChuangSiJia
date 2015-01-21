using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using CourseCenter.Models;
using CourseCenter.Common;
using System.Data.SqlClient;


namespace CourseCenter.Controllers
{
    public class LearnerManageController : Controller
    {
        DBEntities db = new DBEntities();
        string teacherId = TakeCookie.GetCookie("userId");
        ModelHelpers mHelp = new ModelHelpers();

        #region 查询出该教师所有的课程 放在下拉框中，列表中默认显示第一门课程的学生情况+Learners
        /// <summary>
        /// 显示所有的信息，这个是初始化的，首先显示教师的正在上课
        /// 的一门的学生
        /// 获得第一个~~~top 1，where 开课状态为正在上课，课程的学生的信息
        /// </summary>
        /// <returns></returns>
        public ActionResult Learners()
        {
            //查询出该老师所有的课程，根据开始时间排序
            List<Course> listCourse = db.Course.Where(c => c.TeacherId == new Guid(teacherId)).OrderBy(c => c.BeginTime).ToList();
            ViewBag.listCourse = listCourse;
            if (listCourse.Count > 0)
            {
                //页面默认显示第一门课程
                Course firsrCourse = listCourse[0] as Course;
                List<StudentInfo> listStudentInfoOfIndex = db.StudentInfo.SqlQuery("select * from StudentInfoes where Id in (select StudentId from Stu_Course where CourseId=@Id )", new SqlParameter("@Id", firsrCourse.Id)).ToList();
                ViewBag.listStudentInfoOfIndex = listStudentInfoOfIndex;
                ViewBag.CourseId = firsrCourse.Id;
            }
            return View();
        }

        #endregion

        #region 根据点击下拉框的值 获取对应课程的学生情况+GetCourse
        public ActionResult GetCourse(int id)
        {
            List<StudentInfo> listStudentInfoOfIndex = db.StudentInfo.SqlQuery("select * from StudentInfoes where Id in (select StudentId from Stu_Course where Stu_Course.CourseId=@id )", new SqlParameter("@Id", id)).ToList();
            ViewBag.listStudentInfoOfIndex = listStudentInfoOfIndex;
            //查询出该老师所有的课程，根据开始时间排序
            List<Course> listCourse = db.Course.Where(c => c.TeacherId == new Guid(teacherId)).OrderBy(c => c.BeginTime).ToList();
            ViewBag.listCourse = listCourse;
            ViewBag.CourseId = id;
            return View("Learners");
        }
        #endregion

        #region 教师查看学生提交的作业情况+GetStudentWork
        /// <summary>
        /// 获得学生的上传作业的情况
        /// </summary>
        /// <returns></returns>
        public ActionResult GetStudentWork(string moduleTag)
        {
            Guid studentId = new Guid(Request.QueryString["SId"]);
            int courseId = Convert.ToInt32(Request.QueryString["CId"]);
            //;
            //前台界面上需要显示如下:模块名称，作业下载，学生作业内容，题目及学生答案
            ViewBag.courseid = courseId;
            //返回学生的最后得分
            int mTag = 1;//如果是第一次其他页面请求过来，mTag的值为1，则默认显示该课程的第一个模块
            if (!string.IsNullOrEmpty(moduleTag))
            {
                mTag = Convert.ToInt32(moduleTag);
            }
            //返回模块详情
            Module module = db.Module.Where(m => m.CourseId == courseId && m.ModuleTag == mTag).FirstOrDefault();
            ViewBag.module = module;
            //返回学生作业
            StudentWork sWork = db.StudentWork.Where(sw => sw.CourseId == courseId && sw.StudentId == studentId && sw.ModuleTag == mTag).FirstOrDefault();
            ViewBag.sWork = sWork;
            //返回学生问题及答案和评分
            if (module != null)
            {
                List<Erecord> listErecord = mHelp.SqlQuery<Erecord>("select pq.QTitle,psa.Answer,psa.AnswerScore from PaperStudentAnswers as psa join PaperQuestions as pq on psa.QuestionId=pq.Id where psa.MouduleTag=@mTag and psa.StudentId=@studentId", new SqlParameter[] { new SqlParameter("@mTag", mTag), new SqlParameter("@studentId", studentId) }).ToList();
                ViewBag.listErecord = listErecord;
            }
            //todo---查询评价量表的题目
            List<EvaluateTable> EvTableList = db.EvaluateTable.Where(c => c.TableId == 1).ToList();
            ViewBag.EvTableList = EvTableList;

            List<StudentWork> listStudentWork = db.StudentWork.Where(sw => sw.CourseId == courseId && sw.StudentId == studentId).ToList();
            ViewBag.listStudentWork = listStudentWork;

            //这一部分有个问题就是前台那个下拉框

            return View();
        }

        #endregion

        #region 完成教师评价功能+FinishComment()
        /// <summary>
        /// 完成教师评价功能
        /// </summary>
        /// <returns></returns>
        public ActionResult FinishComment()
        {
            Guid studentId = new Guid(Request.QueryString["SId"]);
            int courseId = Convert.ToInt32(Request.QueryString["CId"]);
            int moudultTag = Convert.ToInt32(Request.QueryString["mouduleTag"]);
            //查询出量表的题目ID并且遍历
            List<EvaluateTable> EvTableList = db.EvaluateTable.Where(c => c.TableId == 1).ToList();
            TcComment TComments = null;
            foreach (EvaluateTable EvTable in EvTableList)
            {
                TComments = new TcComment()
                {
                    CourseId = courseId,
                    StudentId = studentId,
                    MouduleTag = moudultTag,
                    TeacherId = new Guid(teacherId),
                    EvTableId = EvTable.Id,
                    TcAnswer = Request.Form[EvTable.Id + "&r"]
                };
                double d = 0.0;
                if (double.TryParse(Request.Form[EvTable.Id + "&r"].ToString(), out d))
                {
                    TComments.TScore = d;
                }
                else
                    TComments.TScore = 0.0;

                db.TcComment.Add(TComments);
            }
            //将评语添加进学生作业评语中

            CouScore couScore = db.CouScore.Where(c => c.CourseId == courseId && c.ModuleTag == moudultTag).FirstOrDefault();
            if (couScore != null)
            {
                if (Request.Form["ModuleComment"] != null)
                {
                    couScore.ModuleComment = Request.Form["ModuleComment"];
                }
                else
                {
                    couScore.ModuleComment = null;
                }
                db.CouScore.Attach(couScore);
            }
            else
            {
                //数据库填写完全即可
            }


            db.SaveChanges();


            return RedirectToAction("Learners");
        }
        #endregion


        #region 教师设置学生成绩+SetScore
        public string SetScore(FormCollection form)
        {
            int courseId = Convert.ToInt32(form["CourseId"]);
            Guid studentId = new Guid(form["StudentId"]);
            int moduleTag = Convert.ToInt32(form["ModuleTag"]);

            CouScore cScore = db.CouScore.Where(c => c.CourseId == courseId && c.StudentId == studentId && c.ModuleTag == moduleTag).FirstOrDefault();
            if (cScore != null)
            {
                cScore.ModuleScore = form["score"];
            }
            else
            {
                cScore = new CouScore()
                {
                    CourseId = courseId,
                    StudentId = studentId,
                    ModuleTag = moduleTag,
                    ModuleScore = form["score"]
                };
                db.CouScore.Add(cScore);

            }
            db.SaveChanges();
            return "OK";
        }
        #endregion


        #region 教师查看学生成绩+GetStudentScore
        public ActionResult GetStudentScore()
        {
            Guid studentId = new Guid(Request.QueryString["SId"]);
            int courseId = Convert.ToInt32(Request.QueryString["CId"]);
            List<CouScore> listCs = db.CouScore.Where(cs => cs.CourseId == courseId && cs.StudentId == studentId).OrderBy(cs => cs.ModuleTag).ToList();
            ViewBag.listCs = listCs;
            string courseName = db.Course.Where(c => c.Id == courseId).FirstOrDefault().CourseName;
            ViewBag.courseName = courseName;

            //计算成绩并显示学生各个部分的
            SysScore score = GetSysScore(studentId, new Guid(teacherId), courseId);
            //ViewData["SysScore"] = score;
            ScoreHelper scoreHelper = new ScoreHelper();

            //试图使用sql来计算课后题失败了
            //List<double> pAnswerList = db.PaperStudentAnswer.SqlQuery("select sum(AnswerScore) from PaperStudentAnswers where CourseId=@courseId and StudentId=@studentId group by MouduleTag", new SqlParameter[]{
            //    new SqlParameter("@courseId",courseId),
            //    new SqlParameter("@studentId",studentId)
            //});

            //陈晓红同学，我不会做的是从PaperStudentAnswer表中，查询出各个模块的分数之和，然后根据比例计算出整体的课后题分数，这个分数直接赋给下面ObjectiveScore方法即可，整个分数就可以计算出来了

            scoreHelper.ObjectiveScore(score, 90);
            List<double> listMoudule = new List<double>();
            int left = listCs.Count<CouScore>();
            //认为模块的成绩是顺序的，不存在跳跃式的成绩评价，只会出现123，不出现135
            for (int i = 0; i <= 4; i++)
            {
                if (i >= left)
                {
                    listMoudule.Add(0.0);
                }
                else if (listCs[i].ModuleTag == i + 1)
                {
                    listMoudule.Add(Convert.ToDouble(listCs[i].ModuleScore));
                }
            }
            double AllScore = scoreHelper.TerminateScore(listMoudule, teacherId, courseId);
            ViewData["AllScore"] = AllScore;
            return View(score);
        }
        #endregion

        #region 获取系统评价部分的信息+GetSysScore(Guid studentId, Guid teacherId, int courseId)
        /// <summary>
        /// 获取系统评价部分的信息
        /// </summary>
        /// <param name="studentId"></param>
        /// <param name="teacherId"></param>
        /// <param name="courseId"></param>
        /// <returns></returns>
        public SysScore GetSysScore(Guid studentId, Guid teacherId, int courseId)
        {
            //获得学生创建博客数量
            int countBlog = db.BlogTitle.SqlQuery("select * from BlogTitles t where t.CourseId=@courseId and t.CreatId=@studentId", new SqlParameter[] { 
                new SqlParameter("@courseId", courseId),
                new SqlParameter("@studentId",studentId)
            }).Count<BlogTitle>();
            //获得学生创建小组数量
            int countGroup = db.CourseGroupTitle.SqlQuery("select * from CourseGroupTitles t where t.CourseId=@courseId and t.CourseGroupCreatId=@studentId", new SqlParameter[] { 
                new SqlParameter("@courseId", courseId),
                new SqlParameter("@studentId",studentId)
            }).Count<CourseGroupTitle>();
            //获得学生消息数量
            int countMsg = db.Question.SqlQuery("select * from Questions t where t.FromId=@studentId and t.ToId=@teacherId", new SqlParameter[] { 
                new SqlParameter("@studentId", studentId),
                new SqlParameter("@teacherId",teacherId)
            }).Count<Question>();
            SysScore sysScore = new SysScore()
            {
                CreatMsgCount = countMsg,
                CreatGroupCount = countGroup,
                CreatBlogCount = countBlog
            };
            return sysScore;
        }
        #endregion

    }
}

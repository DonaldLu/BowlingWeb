﻿using ClosedXML.Excel;
using Dapper;
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Web;

namespace BowlingWeb.Models
{
    public class MemberRepository : IMemberRepository, IDisposable
    {
        private IDbTransaction Transaction { get; set; }
        private IDbConnection conn;
        public MemberRepository()
        {
            string memberConnection = ConfigurationManager.ConnectionStrings["MemberConnection"].ConnectionString;
            conn = new SQLiteConnection(memberConnection);
        }
        // 登入
        public Member Login(Member member)
        {
            Member ret;

            string sql = @"select * from Member where Account=@Account and Password=@Password";
            ret = conn.Query<Member>(sql, member).ToList().FirstOrDefault();

            return ret;
        }
        // 上傳檔案
        public List<Member> Upload(HttpPostedFileBase[] files)
        {
            List<Member> memberList = new List<Member>();
            try
            {
                if (files == null || files.First() == null) throw new ApplicationException("未選取檔案或檔案上傳失敗");
                if (files.Count() != 1) throw new ApplicationException("請上傳單一檔案");
                var file = files.First();
                if (Path.GetExtension(file.FileName) != ".xlsx") throw new ApplicationException("請使用Excel 2007(.xlsx)格式");
                var stream = file.InputStream;
                XLWorkbook wb = new XLWorkbook(stream);
                if (wb.Worksheets.Count > 1)
                {
                    throw new ApplicationException("Excel檔包含多個工作表");
                }
                // 讀取第一個 Sheet
                IXLWorksheet worksheet = wb.Worksheets.Worksheet(1);
                // 定義資料起始/結束 Cell
                var firstCell = worksheet.FirstCellUsed();
                var lastCell = worksheet.LastCellUsed();
                // 使用資料起始/結束 Cell，來定義出一個資料範圍
                var data = worksheet.Range(firstCell.Address, lastCell.Address);
                // 將資料範圍轉型
                var table = data.AsTable();
                //讀取資料
                int columnCount = worksheet.Columns().Count();
                int rowCount = worksheet.Rows().Count();
                for (int i = 3; i <= columnCount; i++)
                {
                    Member member = new Member();
                    member.Name = table.Cell(1, i).Value.ToString();
                    string date = string.Empty;
                    string scores = string.Empty;
                    for (int j = 2; j < rowCount; j++)
                    {
                        // 讀取日期
                        if (table.Cell(j, 2).Value.ToString() != "")
                        {
                            DateTime dateTime = Convert.ToDateTime(table.Cell(j, 2).Value.ToString());
                            date = dateTime.ToString("yyyy/MM/dd");
                        }
                        // 先確認有分數, 才紀錄
                        if (table.Cell(j, i).Value.ToString() != "-")
                        {
                            if (table.Cell(j, 2).Value.ToString() != "")
                            {
                                if(scores.Length > 0)
                                {
                                    scores = scores.Remove(scores.Length - 1, 1) + ";";
                                }
                                scores += date + ":";
                            }
                            else
                            {
                                // 搜尋scores裡是否已記錄了這個日期
                                if(!scores.Contains(date + ":"))
                                {
                                    scores = scores.Remove(scores.Length - 1, 1) + ";";
                                    scores += date + ":";
                                }
                            }
                            if(table.Cell(j, i).Value.ToString() != "")
                            {
                                // 讀取scores目前最後紀錄的日期
                                scores += table.Cell(j, i).Value.ToString() + ",";
                            }
                        }
                    }
                    scores = scores.Remove(scores.Length - 1, 1) + ";";

                    member.Scores = scores;
                    memberList.Add(member);
                }

                return memberList;
            }
            catch (Exception)
            {
                //return Content($"<script>alert({JsonConvert.SerializeObject(ex.Message)})</script>", "text/html");
            }

            return memberList;
        }
        // 讀取檔案
        public List<Member> ReadExcel(string filePath)
        {
            List<Member> memberList = new List<Member>();
            
            // 檔案路徑
            filePath = Path.Combine(HttpContext.Current.Server.MapPath("~/App_Data"), $"Data.xlsx");
            Application app = new Application();
            Sheets sheets;
            object oMissiong = System.Reflection.Missing.Value;
            Workbook workbook = app.Workbooks.Open(filePath, oMissiong, oMissiong);
            System.Data.DataTable dt = new System.Data.DataTable();
            try
            {
                if (app == null) return null;
                workbook = app.Workbooks.Open(filePath, oMissiong, oMissiong);
                sheets = workbook.Worksheets;
                //將資料讀入到DataTable中
                Worksheet worksheet = (Worksheet)sheets.get_Item(1);//請取第一張表
                if (worksheet == null) return null;
                int iRowCount = worksheet.UsedRange.Rows.Count;
                int iColCount = worksheet.UsedRange.Columns.Count;
                //生成列頭
                for (int i = 0; i < iColCount; i++)
                {
                    var name = "column" + i; 
                    var txt = ((Range)worksheet.Cells[1, i + 1]).Text.ToString();
                    if (!string.IsNullOrWhiteSpace(txt)) name = txt;
                    while (dt.Columns.Contains(name)) name = name + "1";//重複行名稱會報錯。
                    dt.Columns.Add(new DataColumn(name, typeof(string)));
                }

                //生成行資料
                Range range;
                int rowIdx = 1;
                for (int iRow = rowIdx; iRow <= iRowCount; iRow++)
                {
                    DataRow dr = dt.NewRow();
                    for (int iCol = 1; iCol <= iColCount; iCol++)
                    {
                        range = (Range)worksheet.Cells[iRow, iCol];
                        dr[iCol - 1] = (range.Value2 == null) ? "" : range.Text.ToString();
                    }

                    dt.Rows.Add(dr);
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                workbook.Close(false, oMissiong);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                workbook = null;
                app.Workbooks.Close();
                app.Quit();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(app);
                app = null;
            }

            return memberList;
        }
        // 個人紀錄
        public Member GetMember(string account)
        {
            Member ret;

            string sql = @"select * from Member where Account=@account";
            List<Member> members = conn.Query<Member>(sql, new { account }).ToList();
            ret = conn.Query<Member>(sql, new { account }).ToList().FirstOrDefault();
            foreach (Member member in members)
            {
                SkillScores skillScores = new SkillScores();
                skillScores.Skill = member.Skill;
                string[] scores = member.Scores.Split(',');
                foreach(string score in scores)
                {
                    skillScores.Scores.Add(Convert.ToDouble(score));
                }
                ret.SkillScores.Add(skillScores);
            }

            return ret;
        }

        public List<Member> GetAll()
        {
            List<Member> ret;

            string sql = @"select * from Member where Name != 'NULL' order by Name";
            ret = conn.Query<Member>(sql).ToList();

            return ret;
        }
        // 註冊
        public Member Create(Member member)
        {
            Member ret;

            string sql = @"INSERT INTO Member VALUES (@Account, @Password, @Name, @Name, @Email, @Email, @Email)";
            ret = conn.Query<Member>(sql, member).ToList().SingleOrDefault();

            return ret;
        }
        public List<Member> GetMember()
        {
            List<Member> ret;
            string sql = @"select * from user where duty != 'NULL' order by dutyName";
            ret = conn.Query<Member>(sql).ToList();

            return ret;
        }
        public void Dispose()
        {
            conn.Close();
            conn.Dispose();
            return;
        }
    }
}
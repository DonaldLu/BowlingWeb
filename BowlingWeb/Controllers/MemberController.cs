﻿using BowlingWeb.Filters;
using BowlingWeb.Models;
using ClosedXML.Excel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace BowlingWeb.Controllers
{
    //[MyAuthorize]
    public class MemberController : Controller
	{
		private MemberService _service;

		public MemberController()
		{
			_service = new MemberService();
		}
		// 首頁
		public ActionResult Index()
        {
            return View();
        }
        // 讀取資料
        public ActionResult Read()
        {
            return View();
        }
        // 註冊
        public ActionResult Register()
        {
            return View();
        }
        // 登入
        public ActionResult Login()
        {
            return View();
        }
        // 個人紀錄
        public ActionResult Record()
        {
            @Session["Account"] = "Donald";
            return View();
        }
        // 統計圖表
        public ActionResult Chart()
        {
            return View();
        }

		// =============== Web API ================

        // 註冊
        [HttpPost]
        public JsonResult CreateMember(Member member)
        {                
            var ret = _service.CreateMember(member);
            return Json(ret);
        }
        // 登入
        [HttpPost]
        public JsonResult Login(Member member)
        {
            var ret = _service.Login(member);
            // 把登入者的資料傳進Session["Account"]做紀錄
            Session["Account"] = ret.Account;

            return Json(ret);
        }
        // 讀取資料
        [HttpPost]
        public JsonResult ReadData(IEnumerable<HttpPostedFileBase> excelFile, string callback)
        {
            var ret = _service.ReadData(excelFile, callback);
            return Json(ret);
        }
        // 個人紀錄
        [HttpPost]
        public JsonResult GetMember(string account)
        {
            account = @Session["Account"].ToString();
            var ret = _service.GetMember(account);
            return Json(ret);
        }
        [HttpPost]
        public JsonResult GetAllMember()
        {
            var ret = _service.GetAllMember();
            return Json(ret);
        }
    }
}
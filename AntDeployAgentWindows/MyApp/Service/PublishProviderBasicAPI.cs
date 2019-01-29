﻿using AntDeployAgentWindows.WebApiCore;
using AntDeployAgentWindows.WebSocketApp;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;

namespace AntDeployAgentWindows.MyApp.Service
{
    public abstract class PublishProviderBasicAPI : CommonProcessor, IPublishProviderAPI
    {
        private object obj = new object();
        private WebSocketApp.WebSocket WebSocket;
        private static readonly ConcurrentDictionary<string, ReaderWriterLockSlim> locker = new ConcurrentDictionary<string, ReaderWriterLockSlim>();
        public abstract string ProviderName { get; }
        public abstract string ProjectName { get; }
        public abstract string DeployExcutor(FormHandler.FormItem fileItem);
        public abstract string CheckData(FormHandler formHandler);
        public string Deploy(FormHandler.FormItem fileItem)
        {
            //按照项目名称 不能并发发布
            if (!string.IsNullOrEmpty(ProjectName))
            {
                var key = (ProviderName ?? string.Empty) + ProjectName;
                if (!locker.TryGetValue(key, out var ReaderWriterLockSlim))
                {
                    ReaderWriterLockSlim = new ReaderWriterLockSlim();
                    locker.TryAdd(key, ReaderWriterLockSlim);
                }

                if (ReaderWriterLockSlim.IsWriteLockHeld)
                {
                    return $"{ProjectName} is deploying!,please wait for senconds!";
                }

                ReaderWriterLockSlim.EnterWriteLock();

                try
                {
                    return DeployExcutor(fileItem);
                }
                finally
                {
                    ReaderWriterLockSlim.ExitWriteLock();
                }

            }
            else
            {
                return DeployExcutor(fileItem);
            }
        }


        public string Check(FormHandler formHandler)
        {
            var wsKey = formHandler.FormItems.FirstOrDefault(r => r.FieldName.Equals("wsKey"));
            if (wsKey != null  && !string.IsNullOrEmpty(wsKey.TextValue))
            {
                var _wsKey = wsKey.TextValue;
                MyWebSocketWork.WebSockets.TryGetValue(_wsKey, out WebSocket);
            }

            return CheckData(formHandler);
        }

        protected void EnsureProjectFolder(string path)
        {
            try
            {
                lock (obj)
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        protected void Log(string str)
        {
            try
            {
                if (WebSocket != null)
                {
                    WebSocket.Send(str + "@_@" + str.Length);
                }
            }
            catch (Exception)
            {
                //ignore

            }
        }

        protected string getCorrectFolderName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(System.Char.ToString(c), "");
            return name;
        }
    }


}

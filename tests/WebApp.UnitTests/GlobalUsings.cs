global using System;
global using System.Linq;
global using System.Net;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
global using eShop.WebApp.Services;
global using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

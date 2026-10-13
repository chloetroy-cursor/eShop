global using System;
global using System.IO;
global using System.Net;
global using System.Threading.Tasks;
global using System.Net.Sockets;
global using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

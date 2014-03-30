using System;
using System.Net;
using System.Text;

namespace Promises
{
  class HttpServerSync
  {
    // Adapted from http://msdn.microsoft.com/en-us/library/system.net.httplistener(v=vs.110).aspx.
    // To test this curl http://localhost:9080/.
    // Sync impl which sends exactly one response to one client and then terminates:
    public static void run()
    {
      HttpListener listener = new HttpListener();
      listener.Prefixes.Add("http://localhost:9080/");
      listener.Start();
      Console.WriteLine("Listening...");

      while (true)
      {
        HttpListenerContext context = listener.GetContext(); // The GetContext method blocks while waiting for a request. 
        Console.WriteLine("processing request ...");
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;
        bool isShutdownRequest = processRequest(request, response);
        if (isShutdownRequest)
          break;
        Console.WriteLine("done processing request.");
      }

      listener.Stop();
    }

    // @return true if this is a shutdown request (http://localhost:9080/shutdown)
    static bool processRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
      if (request.Url.PathAndQuery == "/shutdown")
        return true;

      // simulate long running I/O here, e.g. a DB request
      int dbQueryResult = simulatedDatabaseRequest();

      string responseString = "<HTML><BODY> Hello world! Request #" + dbQueryResult + "</BODY></HTML>";
      byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
      response.ContentLength64 = buffer.Length;
      System.IO.Stream output = response.OutputStream;
      output.Write(buffer, 0, buffer.Length);
      output.Close();
      return false;
    }

    static int simulatedDatabaseRequest()
    {
      // simulate long running I/O here, e.g. a DB request
      System.Threading.Thread.Sleep(3000); 
      return ++requestX; // not threadsafe, whatever
    }

    static int requestX = 0;
  }
}

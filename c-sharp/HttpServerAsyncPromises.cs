using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Promises
{
  // Instead of using the abominable AsyncCallback + IAsyncResult + Begin/EndBla() methods this impl here 
  // Promises aka .NET Task + BlaAsync() methods. Compared to the callback version this is much less boilerplate.
  // But still a long way from the less verbose nodejs promises.
  class HttpServerAsyncPromises
  {
    public static void run()
    {
      HttpListener listener = new HttpListener();
      listener.Prefixes.Add("http://localhost:9080/");
      listener.Start();
      Console.WriteLine("Listening...");

      // This listener.BeginGetContext only registers a callback for a single HTTP request.
      staticHttpListener = listener; // fugly: just so that the processGetContextResult() handler can trigger another GetContextAsync call.
      listener.GetContextAsync().ContinueWith(processGetContextResult);

      // this GetContextAsync() only registered a callback, if we don't prevent the process
      // from terminating explicitly here it will exit right away.
      // Isnt it kinda ugly that we have to WaitOne() here? Would be nice to make a call to pump
      // the message queue?
      shutdownRequest.WaitOne();
      listener.Stop();
    }

    static HttpListener staticHttpListener; // fugly: is there any way to avoid this? This stems from GetContextAsync() only registering a callback for a single request, whereas a nodejs server registers a callback for a whole series of requests.

    static void processGetContextResult(Task<HttpListenerContext> task) 
    {
      HttpListenerContext context = (HttpListenerContext)task.Result;
      HttpListenerRequest request = context.Request;
      HttpListenerResponse response = context.Response;

      // In a Nodejs HTTP server you register one callback function for all future requests.
      // But a listener.GetContextAsync() will only register one call back for the next immediate request.
      // What if a request comes in while no callback is registered, will it be buffered in a queue? No idea,
      // I hope so. In any case, we have to register a callback for the next HTTP request. Fugly imo.
      staticHttpListener.GetContextAsync().ContinueWith(processGetContextResult);
        
      processRequest(request, response);
    }

    // set when /shutdown route is triggered by a client
    static System.Threading.AutoResetEvent shutdownRequest = new System.Threading.AutoResetEvent(false);

    static void processRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
      if (request.Url.PathAndQuery == "/shutdown")
      {
        shutdownRequest.Set();
        return;
      }

      // simulate long running I/O here, e.g. a DB request
      simulatedDatabaseRequest().ContinueWith((Task<int> task) =>
      {
        int dbQueryResult = (int)task.Result;
        string responseString = "<HTML><BODY> Hello world! Request #" + dbQueryResult + "</BODY></HTML>";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        System.IO.Stream output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
      });
    }

    static Task<int> simulatedDatabaseRequest()
    {
      // simulate long running I/O here, e.g. a DB request
      return Task.Delay(3000).ContinueWith((Task task) =>
      {
        return ++requestX; // not threadsafe, whatever
      });
    }

    static int requestX = 0;
  }
}

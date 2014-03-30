using System;
using System.Net;
using System.Text;

namespace Promises
{
  // This uses the AsyncCallback + IAsyncResult + Begin/EndBla() methods to use an async programming model.
  // This model works extremely poorly with .NET imo: lots of awkward boilerplate garbage, so compared
  // to nodejs callbacks this here is a nightmare.
  class HttpServerAsyncCallback
  {
    public static void run()
    {
      HttpListener listener = new HttpListener();
      listener.Prefixes.Add("http://localhost:9080/");
      listener.Start();
      Console.WriteLine("Listening...");

      // This listener.BeginGetContext only registers a callback for a single HTTP request.
      IAsyncResult result = listener.BeginGetContext(new AsyncCallback(getContextCallback), listener);

      // this BeginGetContext() only registered a callback, if we don't prevent the process
      // from terminating explicitly here it will exit right away.
      // Isnt it kinda ugly that we have to WaitOne() here? Would be nice to make a call to pump
      // the message queue?
      shutdownRequest.WaitOne();
      listener.Stop();
    }

    static void getContextCallback(IAsyncResult result) 
    {
      HttpListener listener = (HttpListener)result.AsyncState;
      HttpListenerContext context = listener.EndGetContext(result);
      HttpListenerRequest request = context.Request;
      HttpListenerResponse response = context.Response;

      // In a Nodejs HTTP server you register one callback function for all future requests.
      // But a listener.BeginGetContext() will only register one call back for the next immediate request.
      // What if a request comes in while no callback is registered, will it be buffered in a queue? No idea,
      // I hope so. In any case, we have to register a callback for the next HTTP request. Fugly imo.
      listener.BeginGetContext(new AsyncCallback(getContextCallback), listener);

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
      beginSimulatedDatabaseRequest(new AsyncCallback((IAsyncResult asyncResult) =>
      {
        int dbQueryResult = ((Func<int>)asyncResult.AsyncState).EndInvoke(asyncResult);
        string responseString = "<HTML><BODY> Hello world! Request #" + dbQueryResult + "</BODY></HTML>";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        System.IO.Stream output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
      }));
    }

    // See also http://stackoverflow.com/questions/1047662/what-is-asynccallback for this ugly callback model.
    static IAsyncResult beginSimulatedDatabaseRequest(AsyncCallback callback)
    {
      // simulate long running I/O here, e.g. a DB request
      var func = new Func<int>(() => {
        System.Threading.Thread.Sleep(3000);
        return ++requestX; // not threadsafe, whatever
      });
      return func.BeginInvoke(callback, func); // which thread will this execute on? A threadpool?
    }

    static int requestX = 0;
  }
}

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Promises
{
  class HttpServerAsyncAwait
  {
    // Promises with Async/Await.
    // Note that this here doesnt work: "public async static void run()", 
    // see http://blog.stephencleary.com/2012/02/async-console-programs.html.
    public static void run()
    {
      // from http://blog.stephencleary.com/2012/02/async-console-programs.html
      Nito.AsyncEx.AsyncContext.Run(() => runAsync());
    }

    public async static void runAsync() {
      HttpListener listener = new HttpListener();
      listener.Prefixes.Add("http://localhost:9080/");
      listener.Start();
      Console.WriteLine("Listening...");

      while (true)
      {
        HttpListenerContext context = await listener.GetContextAsync();
        Console.WriteLine("processing request ...");
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;
        bool isShutdownRequest = await processRequestAsync(request, response);
        if (isShutdownRequest)
          break;
        Console.WriteLine("done processing request.");
      }

      listener.Stop();
    }

    // @return true if this is a shutdown request (http://localhost:9080/shutdown)
    static async Task<bool> processRequestAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
      if (request.Url.PathAndQuery == "/shutdown")
        return true;

      // simulate long running I/O here, e.g. a DB request
      int dbQueryResult = await simulatedDatabaseRequestAsync();

      string responseString = "<HTML><BODY> Hello world! Request #" + dbQueryResult + "</BODY></HTML>";
      byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
      response.ContentLength64 = buffer.Length;
      System.IO.Stream output = response.OutputStream;
      output.Write(buffer, 0, buffer.Length);
      output.Close();
      return false;
    }

    static Task<int> simulatedDatabaseRequestAsync()
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

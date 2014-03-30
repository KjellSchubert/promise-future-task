using System;
using System.Net;
using System.Text;

namespace Promises
{
  class Program
  {
    // Adapted from http://msdn.microsoft.com/en-us/library/system.net.httplistener(v=vs.110).aspx.
    // To test this curl http://localhost:9080/.
    static void Main()
    {
      // Enable one or several of these alternative implementations:

      //HttpServerSync.run();
      HttpServerAsyncCallback.run();
      //HttpServerAsyncPromises.run();
      //HttpServerAsyncAwait.run();
    }
  }
}

#include <iostream>
#include <string>
#include <fstream>
#include <future>
#include <thread>
#include <exception>
using namespace std;

int countLinesSync(string fileName) {
  ifstream myfile(fileName, std::ifstream::in);
  if (!myfile.good())
    throw runtime_error ("error opening " + fileName);
  int lineCount = 0;
  string line;
  while (getline(myfile, line))
    ++lineCount;
  return lineCount;
}

// should be a template, and also should be part of the C++ standard some day:
future<void> then(future<int>& first, std::function<void(int)> second) {
  // horribly inefficient implementation spawning yet another thread
  auto future = std::async(std::launch::async, [&](){ second(first.get()); }); // capture by reference is likely a bug, should rather capture by value? Use shared_future?
  return future;
}

int main()
{
  try {
    string fileName = "promises.cpp";
    cout << "sync call: lineCount=" << countLinesSync(fileName) << endl;

    auto future1 = std::async(std::launch::async, [=](){ return countLinesSync(fileName); });
    auto future2 = then(future1, [=](int lineCount){ cout << "async call: lineCount=" << lineCount; });
    future2.wait(); // if C++ had a concept like Python's event loops then we'd wait for the loop to finish here instead.
    return 0;
  }
  catch (exception ex) {
    cout << "exception: " << ex.what();
    return -1;
  }
}
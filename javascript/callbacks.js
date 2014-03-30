var fs = require('fs');

"use strict";

// @param callback(err, len) will receive the file length
function getFileLengthInKb(fileName, callback) {
  fs.readFile(fileName, {encoding:'utf8'}, function(err, fileContent) {
    if (err) {
        callback(err);
    }
    var len = fileContent.length / 1024;
    callback(undefined, len);
  });
}

var testFileName = "package.json";
getFileLengthInKb(testFileName, function(err, fileLength) {
    if (err) {
        throw err;
    }
    console.log('length  of ' + testFileName + ' is ' + fileLength + ' kB');
});
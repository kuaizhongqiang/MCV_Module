mergeInto(LibraryManager.library, {
  CloseWindow: function () {
    // 尝试调用浏览器的关闭窗口方法
    window.close();
  }
});
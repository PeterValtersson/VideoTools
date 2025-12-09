console.log("Background loaded");

chrome.runtime.onMessage.addListener((msg) => {
  console.log("Message received", msg);
});
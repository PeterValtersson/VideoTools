
const url = "http://localhost:5000/video-tools/downloader";

document.getElementById("downloadc").onclick = function(){download(getCurrentUrlTitle, "/add")};
document.getElementById("recordc").onclick = function(){download(getCurrentUrlTitle, "record")};
document.getElementById("download").onclick = function(){download(getInputUrlTtile, "/add")};
document.getElementById("record").onclick = function(){download(getInputUrlTtile, "record")};

document.getElementById("start").onclick = start;
document.getElementById("remove").onclick = remove;
document.getElementById("pause").onclick = pause;

const urlBox = document.getElementById("urlBox");
document.getElementById("body").onmouseenter = function(){urlBox.focus(); urlBox.value = ""; document.execCommand("Paste");};

const downlist = document.getElementById("downlist");

setInterval(buildListContent, 1000)

async function buildListContent() 
{
  fetch(url + "/list",
    {
      method: "GET"
    }).then(async res =>
    {
      const raw = await res.text();
      console.log(raw)

      try 
      {
        const json = JSON.parse(raw);
        console.log("Parsed JSON:", json);
        entries = json.data;
        Array.from(downlist.children).forEach(element => {
          if(entries.find((data)=>data.name+'liid' == element.id) == undefined)
            element.outerHTML = "";
        });

        entries.forEach(entryData => 
        {
          var liid = entryData.name+'liid';
          if(Array.from(downlist.children).find((data)=>data.id == liid) != undefined)
          {
            document.getElementById(entryData.name + "litextid").innerHTML = entryData.name + " " + entryData.status;
            return;
          }
          
          const cli = document.getElementById(liid);
          if(cli != null)
            return;
          console.log(entryData.name);
          let input = document.createElement('input');
          input.setAttribute('type', 'checkbox');
          input.setAttribute('id', entryData.name + "licbid");
      
          let text = document.createElement('span');
          text.setAttribute('id', entryData.name+"litextid");
          text.innerHTML = entryData.name + " " + entryData.status;

          let label = document.createElement('label');
          label.appendChild(input);
          label.appendChild(text);
          label.setAttribute('for', entryData.name + "licbid");
          
          let li = document.createElement('li');
          li.setAttribute('id', entryData.name+'liid');
          li.appendChild(label);
          li.setAttribute('data-url', entryData.url)
          li.setAttribute('data-name', entryData.name)
          downlist.appendChild(li);
        });
      } catch (e) 
      {
        console.warn("Not valid JSON");
      }
    });
}

async function getCurrentUrlTitle()
{
  const [tab] = await chrome.tabs.query({active: true, lastFocusedWindow: true});
  console.log(tab.url);
  console.log(tab.title);
  return [tab.url, tab.title];
}

async function getInputUrlTtile()
{
  console.log(urlBox.value);
  return [urlBox.value, urlBox.value];
}

async function download(urlTitleGetter, type)
{
  const [pageUrl, title] = await urlTitleGetter();
  console.log("Adding new download task")
  fetch(url + type,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({ name: title, url: pageUrl, taskOptions: "RemoveOnFinish"  })
    }
  ).then((response) => console.log(response.text))
}

async function start()
{
  Array.from(downlist.children).forEach(element => {
    if(document.getElementById(element.dataset.name + "licbid").checked)
      fetch(url + "start",
        {
          method: "POST",
          body: JSON.stringify({ name: element.dataset.name, url: element.dataset.url })
        }
      ).then((response) => console.log(response.json.text))
  });

}

async function pause()
{
  Array.from(downlist.children).forEach(element => {
    if(document.getElementById(element.dataset.name + "licbid").checked)
      fetch(url + "pause",
        {
          method: "POST",
          body: JSON.stringify({ name: element.dataset.name, url: element.dataset.url })
        }
      ).then((response) => console.log(response.json.text))
  });

}


async function remove()
{
  Array.from(downlist.children).forEach(element => {
    if(document.getElementById(element.dataset.name + "licbid").checked)
      fetch(url + "remove",
        {
          method: "POST",
          body: JSON.stringify({ name: element.dataset.name, url: element.dataset.url })
        }
      ).then((response) => console.log(response.json.text))
    
  });

}

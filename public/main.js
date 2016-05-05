function $(str) { if (str[0] === '.') { return document.getElementsByClassName(str.substring(1)); } else if (str[0] === '#') { return document.getElementById(str.substring(1)); } else { return document.getElementsByTagName(str.substring(1)); } }
function co(b){var a={r:1,g:1,b:1};.25>b?(a.r=0,a.g=4*b):.5>b?(a.r=0,a.b=1+4*(.25-b)):(.75>b?a.r=4*(b-.5):a.g=1+4*(.75-b),a.b=0);return a};

//http://stackoverflow.com/questions/18638900/javascript-crc32
var makeCRCTable = function(){
    var c;
    var crcTable = [];
    for(var n =0; n < 256; n++){
        c = n;
        for(var k =0; k < 8; k++){
            c = ((c&1) ? (0xEDB88320 ^ (c >>> 1)) : (c >>> 1));
        }
        crcTable[n] = c;
    }
    return crcTable;
}

var crc32 = function(str) {
    var crcTable = window.crcTable || (window.crcTable = makeCRCTable());
    var crc = 0 ^ (-1);

    for (var i = 0; i < str.length; i++ ) {
        crc = (crc >>> 8) ^ crcTable[(crc ^ str.charCodeAt(i)) & 0xFF];
    }

    return (crc ^ (-1)) >>> 0;
};

function setBG()
{
	var x = Math.random();
	var c = co(x);
	document.documentElement.style.backgroundColor = 'rgb(' + parseInt(255*c.r, 10) + ',' + parseInt(255*c.g, 10) + ',' + parseInt(255*c.b, 10) + ')';
}

function addDropZoneFunctions()
{
	var dz = document.getElementById('upload');
	dz.addEventListener('dragover', handleDragOver, false);
	dz.addEventListener('drop', handleFileSelect, false);
	dz.addEventListener('click', handleDropClick, false);
}

function checkForFrag()
{
	if($('#upload') !== null)
	{
		addDropZoneFunctions();
		addPasteFunctions();
	}
}

function addPasteFunctions()
{
	document.addEventListener('paste', handleFilePaste, false);
}

function uploadComplete(rsp, id, s)
{
	var upl = $('#' + id);
	var upl_p = $('#' + id + '_imagePreview');
	
	//remove progress bar
	var pb = $('#' + id + '_progress');
	pb.parentElement.parentElement.removeChild(pb.parentElement);
	
	//resize box
	upl.style.height = '100px';
	upl.style.lineHeight = '20px';
	if(upl_p !== null)
	{
		upl_p.style.height = '100px';
		upl_p.style.maxWidth = '100px';
	}
	
	//update links etc
	if(rsp !== null)
	{
		switch(rsp.status)
		{
			case 0: {
				//generic error
				break;
			}
			case 1: {
				//udupe
				break;
			}
			case 2: {
				//save failed
				break;
			}
			case 200:{
				//ok
				//upl.innerText = upl.innerText + '<small>' + rsp.hash + '</small>';
				var lk = window.location.host + ((window.location.port !== '80' || window.location.port !== '443') && window.location.port !== '' ? ':' + window.location.port : '') + window.location.pathname + (window.location.pathname.indexOf('/') >= 0 ? '' : '/') + rsp.publichash;
				var upl_t = $('#' + id + '_title');
				upl_t.innerHTML = upl_t.innerHTML 
					+ '<br/><small><b>Hash256:</b> ' + rsp.hash 
					+ '</small><br/><small><b>Hash160:</b> ' + rsp.publichash + '</small>'
					+ '<br/><small><a target=\"_blank\" href=\"//' + lk + '&v\">(link)</a></small>';
				break;
			}
		}
	}
}

function uploadProgress(evt, id)
{
	switch(evt.type){
		case 'readystatechange':{
			if(evt.target.readyState == 4)
			{
				uploadComplete(JSON.parse(evt.target.response), id, 0);
			}
			break;
		}
		case 'progress':{
			var p = parseFloat(evt.loaded) / parseFloat(evt.total);
			var pb = $('#' + id + '_progress');
			pb.style.width = (pb.parentElement.offsetWidth * p) + 'px';			
			break;
		}
		case 'error':{
			break;
		}
	}
}

function changeUI()
{
	if($('#uploads').style.display === 'none')
	{
		//minimize dz
		$('#upload').style.lineHeight = "150px";
		$('#upload').style.height = "167px";
		$('#uploads').style.minHeight = "167px";
		$('#uploads').style.display = "block";	
	}
}

/*
 * Accepts File/Blob type ONLY
*/
function uploadFile(f, id)
{
	if(f instanceof Blob || f instanceof File)
	{
		if($('#' + id) === null){
			var nf = document.createElement('div');
			nf.id = id;
			nf.className = "uploadItem";
			
			//check is image type, add preview pane
			if(f.type.indexOf('image') >= 0)
			{ 
				var pid = id + '_imagePreview';
				var pi = document.createElement('img');
				pi.id = pid;
				pi.className = "previewImage";
				nf.appendChild(pi);
				
				var fr = new FileReader();
				fr.onload = function (res) {
					$('#' + pid).src = res.target.result;
				};
				fr.readAsDataURL(f);
			}
			
			//title
			var nf_t = document.createElement('div');
			nf_t.id = id + '_title';
			nf_t.className = 'uploadTitle';
			nf_t.innerHTML = f.name;
			nf.appendChild(nf_t);
			
			//progress bar
			var nfp = document.createElement('span');
			nfp.className = "progress";
			nf.appendChild(nfp);
			
			//progress bar inner
			var nfp_c = document.createElement('span');
			nfp_c.id = id + '_progress';
			nfp_c.className = "progressCurrent";
			nfp.appendChild(nfp_c);
			
			$('#uploads').appendChild(nf);
			
			changeUI();
			
			if(f.size > max_upload_size)
			{
				uploadComplete(null, id, 1);
			}
			else 
			{
				var xhr = new XMLHttpRequest();
				
				xhr.upload.addEventListener('progress', function(evt) { uploadProgress(evt, id); });
				xhr.upload.addEventListener('load', function(evt) { uploadProgress(evt, id); });
				xhr.upload.addEventListener('error', function(evt) { uploadProgress(evt, id); });
				xhr.upload.addEventListener('abort', function(evt) { uploadProgress(evt, id); });
				xhr.addEventListener('readystatechange', function(evt) { uploadProgress(evt, id); });
				
				xhr.open("POST", "upload.php?filename=" + f.name);
				xhr.send(f);
			}
		}
	}
}

function handleDropClick(evt){
	var i = document.createElement('input');
	i.setAttribute('type', 'file');
	i.addEventListener('change', function(evt){
		var fl = evt.path[0].files;
		for(var i = 0; i < fl.length; i++)
		{
			var file = fl[i];
			
			var fid = crc32(file.name); 
			uploadFile(file, fid);
		}
	});
	i.click();
}

function handleDragOver(evt)
{
	evt.stopPropagation();
	evt.preventDefault();
	evt.dataTransfer.dropEffect = 'copy';
}

function handleFileSelect(evt)
{
	evt.stopPropagation();
	evt.preventDefault();
	
	var files = evt.dataTransfer.files;
	console.log(files);
	
	for(var i = 0; i < files.length; i++){
		var file = files[i];
		
		var fid = crc32(file.name);
		if(file.type === ''){
			file.type = 'application/octet-stream';
		}
		uploadFile(file, fid);	
	}
}

function handleFilePaste(evt)
{
	for(var i = 0; i < evt.clipboardData.items.length; i++)
	{
		var fid = crc32('' + new Date().getTime());
		var file = evt.clipboardData.items[i];
		if(file.kind === 'file')
		{
			var file_t = file.getAsFile();
			file_t.name = "clipboard.png";
			uploadFile(file_t, fid);
		}
	}
}

setBG();
checkForFrag();
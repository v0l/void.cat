const API = {
    xhr: function (method, url, data, cb) {
        let x = new XMLHttpRequest();
        x.onreadystatechange = function () {
            if (x.readyState === 4 && cb !== undefined && cb !== null && typeof cb === 'function') {
                cb(this); 
            }
        }
        x.open(method, url, true);
        if (data !== null) {
            x.setRequestHeader('Content-Type', 'application/json');
            x.send(JSON.stringify(data));
        } else {
            x.send();
        }
    },
	
	sendAPICommand: function (data, cb) {
        API.xhr('POST', '/src/php/api.php', data, function (xhr) {
			if(xhr.status == 200) {
				cb(JSON.parse(xhr.response));
			}
        });
    },

    getServerConfig: function (cb) {
        API.sendAPICommand({ cmd: 'config' }, function (data) {
			cb(data);
        });
    },
	
	getFileInfo: function(hash, cb) {
		API.sendAPICommand({ cmd: 'file', hash: hash }, function (data) {
			cb(data);
        });
	}
};

const Util = {
    formatBytes: function (b, f) {
        f = f === undefined ? 2 : f;
        if (b >= 1073741824) {
            return (b / 1073741824.0).toFixed(f) + ' GiB';
        } else if (b >= 1048576) {
            return (b / 1048576.0).toFixed(f) + ' MiB';
        } else if (b >= 1024) {
            return (b / 1024.0).toFixed(f) + ' KiB';
        }
        return b.toFixed(f | 2) + ' B'
    }
};

const doCaptcha = function(view){
	API.sendAPICommand({ cmd: 'captcha_config' }, function(data){
		this.view.captchaKey = data.cap_key;
		this.view.captchaDL = data.cap_dl;
		
		window['capLoad'] = function(){
			window["capCb"] = function(rsp){
				API.sendAPICommand({ cmd: 'captcha_verify', hash: this.view.fileInfo.hash160, token: rsp }, function(data){
					if(window.location.search.indexOf('?dl') === 0){
						window.location = window.location.href.replace('?dl#', '');
					}else{
						window.location.reload();
					}
				}.bind({ view: this.view }));
			}.bind({ view: this.view });
			
			grecaptcha.render(document.querySelector('#g-recaptcha'), 
				{ 
					sitekey: this.view.captchaKey,
					callback: 'capCb'
				}
			);
		}.bind({ view: this.view });
		let cb = document.createElement('div');
		cb.id = 'g-recaptcha';
		
		let par = document.querySelector('.content');
		par.insertBefore(cb, par.firstChild);
		
		let ct = document.createElement('script');
		ct.src = 'https://www.google.com/recaptcha/api.js?onload=capLoad&render=explicit';
		document.head.appendChild(ct);
	}.bind({ view: view }));
};
import {useEffect, useState} from "react";

export function FilePreview(props) {
    let [info, setInfo] = useState();
    
    async function loadInfo() {
        let req = await fetch(`/upload/${props.id}`);
        if(req.ok) {
            let info = await req.json();
            setInfo(info);
        }
    }
    
    useEffect(() => {
        loadInfo();
    }, []);
    
    return (
      <div>
          {info ? <a href={`/d/${info.id}`}>{info.metadata?.name ?? info.id}</a> : "Not Found"}
      </div>  
    );
}
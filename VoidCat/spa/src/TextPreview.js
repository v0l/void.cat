import {useEffect, useState} from "react";
import "./TextPreview.css";

export function TextPreview(props) {
    let [content, setContent] = useState("Loading..");

    async function getContent(link) {
        let req = await fetch(link);
        if(req.ok) {
            setContent(await req.text());
        }
    }
    
    useEffect(() => {
        getContent(props.link);
    }, []);
    
    return (
        <pre className="text-preview">{content}</pre>
    )
}
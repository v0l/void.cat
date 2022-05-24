import {useEffect, useState} from "react";
import "./TextPreview.css";

export function TextPreview(props) {
    let [content, setContent] = useState("Loading..");

    async function getContent(link) {
        let req = await fetch(`${link}?t=${new Date().getTime()}`, {
            headers: {
                "pragma": "no-cache",
                "cache-control": "no-cache"
            }
        });
        if (req.ok) {
            setContent(await req.text());
        } else {
            setContent("ERROR :(")
        }
    }

    useEffect(() => {
        if (props.link !== undefined && props.link !== "#") {
            getContent(props.link);
        }
    }, [props.link]);

    return (
        <pre className="text-preview">{content}</pre>
    )
}
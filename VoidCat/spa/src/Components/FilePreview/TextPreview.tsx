import {useEffect, useState} from "react";
import "./TextPreview.css";

export function TextPreview({link}: { link: string }) {
    let [content, setContent] = useState("Loading..");

    async function getContent(link: string) {
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
        if (link !== undefined && link !== "#") {
            getContent(link).catch(console.error);
        }
    }, [link]);

    return (
        <pre className="text-preview">{content}</pre>
    )
}
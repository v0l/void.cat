import {Dropzone} from "./Dropzone";
import {GlobalStats} from "./GlobalStats";
import {FooterLinks} from "./FooterLinks";

import "./HomePage.css";

export function HomePage() {
    return (
        <div className="home">
            <Dropzone/>
            <GlobalStats/>
            <FooterLinks/>
        </div>
    );
}
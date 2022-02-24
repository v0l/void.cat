import {Dropzone} from "./Dropzone";
import {GlobalStats} from "./GlobalStats";
import {FooterLinks} from "./FooterLinks";

export function HomePage() {
    return (
        <div className="page">
            <Dropzone/>
            <GlobalStats/>
            <FooterLinks/>
        </div>
    );
}
import {Fragment} from "react";
import {Dropzone} from "./Dropzone";
import {GlobalStats} from "./GlobalStats";
import {FooterLinks} from "./FooterLinks";

export function HomePage(props) {
    return (
        <Fragment>
            <Dropzone/>
            <GlobalStats/>
            <FooterLinks/>
        </Fragment>
    );
}
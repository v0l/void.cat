import {ConstName} from "./Util";
import {PaywallCurrencies} from "./Const";
import {useState} from "react";

export function FilePaywall(props) {
    const file = props.file;
    const pw = file.paywall;
    
    const [order, setOrder] = useState();
    
    async function fetchOrder() {
        let req = await fetch("")
    }
    
    return (
        <div className="paywall">
            <h3>You must pay {ConstName(PaywallCurrencies, pw.cost.currency)} {pw.cost.amount} to view this file.</h3>
            <button onClick={fetchOrder}>Pay</button>
        </div>
    );
}
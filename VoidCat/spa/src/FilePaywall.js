import "./FilePaywall.css";
import {FormatCurrency} from "./Util";
import {PaywallServices} from "./Const";
import {useState} from "react";
import {LightningPaywall} from "./LightningPaywall";
import {useApi} from "./Api";
import {VoidButton} from "./VoidButton";

export function FilePaywall(props) {
    const {Api} = useApi();
    const file = props.file;
    const pw = file.paywall;
    const paywallKey = `paywall-${file.id}`;
    const onPaid = props.onPaid;

    const [order, setOrder] = useState();

    async function fetchOrder() {
        let req = await Api.createOrder(file.id);
        if (req.ok && req.status === 200) {
            setOrder(await req.json());
        }
    }

    function reset() {
        setOrder(undefined);
    }

    function handlePaid(order) {
        window.localStorage.setItem(paywallKey, JSON.stringify(order));
        if (typeof onPaid === "function") {
            onPaid();
        }
    }

    if (!order) {
        return (
            <div className="paywall">
                <h3>
                    You must pay {FormatCurrency(pw.cost.amount, pw.cost.currency)} to view this file.
                </h3>
                <VoidButton onClick={fetchOrder}>Pay</VoidButton>
            </div>
        );
    } else {
        switch (pw.service) {
            case PaywallServices.Strike: {
                return <LightningPaywall file={file} order={order} onReset={reset} onPaid={handlePaid}/>;
            }
        }
        return null;
    }
}
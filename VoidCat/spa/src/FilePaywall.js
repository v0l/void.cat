import {ConstName, FormatCurrency} from "./Util";
import {PaywallCurrencies, PaywallServices} from "./Const";
import {useState} from "react";
import {LightningPaywall} from "./LightningPaywall";

export function FilePaywall(props) {
    const file = props.file;
    const pw = file.paywall;
    const paywallKey = `paywall-${file.id}`;
    const onPaid = props.onPaid;

    const [order, setOrder] = useState();

    async function fetchOrder(e) {
        e.target.disabled = true;
        let req = await fetch(`/upload/${file.id}/paywall`);
        if (req.ok) {
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
                <h3>You must pay {FormatCurrency(pw.cost.amount, pw.cost.currency)} to view this
                    file.</h3>
                <button onClick={fetchOrder}>Pay</button>
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
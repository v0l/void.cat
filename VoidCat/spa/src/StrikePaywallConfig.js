import {useState} from "react";
import FeatherIcon from "feather-icons-react";
import {PaywallCurrencies} from "./Const";

export function StrikePaywallConfig(props) {
    const file = props.file;
    const privateFile = props.privateFile;
    const paywall = file.paywall;
    const editSecret = privateFile.metadata.editSecret;
    const id = file.id;

    const [username, setUsername] = useState(paywall?.handle ?? "hrf");
    const [currency, setCurrency] = useState(paywall?.cost.currency ?? PaywallCurrencies.USD);
    const [price, setPrice] = useState(paywall?.cost.amount ?? 1);
    const [saveStatus, setSaveStatus] = useState();
    
    async function saveStrikeConfig(e) {
        e.target.disabled = true;
        let cfg = {
            editSecret,
            strike: {
                handle: username,
                cost: {
                    currency: currency,
                    amount: price
                }
            }
        };

        let req = await fetch(`/upload/${id}/paywall`, {
            method: "POST",
            body: JSON.stringify(cfg),
            headers: {
                "Content-Type": "application/json"
            }
        });
        if (!req.ok) {
            alert("Error settings paywall config!");
        }
        setSaveStatus(req.ok);
        e.target.disabled = false;
    }

    return (
        <div>
            <dl>
                <dt>Stike username:</dt>
                <dd><input type="text" value={username} onChange={(e) => setUsername(e.target.value)}/></dd>
                <dt>Currency:</dt>
                <dd>
                    <select onChange={(e) => setCurrency(parseInt(e.target.value))} value={currency}>
                        <option value={PaywallCurrencies.BTC}>BTC</option>
                        <option value={PaywallCurrencies.USD}>USD</option>
                        <option value={PaywallCurrencies.EUR}>EUR</option>
                        <option value={PaywallCurrencies.GBP}>GBP</option>
                    </select>
                </dd>
                <dt>Price:</dt>
                <dd><input type="number" value={price} onChange={(e) => setPrice(parseFloat(e.target.value))}/></dd>
            </dl>
            <button onClick={saveStrikeConfig}>Save</button>
            {saveStatus ? <FeatherIcon icon="check-circle"/> : null}
        </div>
    );
}
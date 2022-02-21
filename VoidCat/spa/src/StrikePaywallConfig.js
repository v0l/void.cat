import {useState} from "react";
import {PaywallCurrencies} from "./Const";

export function StrikePaywallConfig(props) {
    const editSecret = props.file.metadata.editSecret;
    const id = props.file.id;

    const [username, setUsername] = useState("hrf");
    const [currency, setCurrency] = useState(PaywallCurrencies.USD);
    const [price, setPrice] = useState(1);

    async function saveStrikeConfig() {
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
    }

    return (
        <div>
            <dl>
                <dt>Stike username:</dt>
                <dd><input type="text" value={username} onChange={(e) => setUsername(e.target.value)}/></dd>
                <dt>Currency:</dt>
                <dd>
                    <select onChange={(e) => setCurrency(parseInt(e.target.value))}>
                        <option selected={currency === PaywallCurrencies.BTC} value={PaywallCurrencies.BTC}>BTC</option>
                        <option selected={currency === PaywallCurrencies.USD} value={PaywallCurrencies.USD}>USD</option>
                        <option selected={currency === PaywallCurrencies.EUR} value={PaywallCurrencies.EUR}>EUR</option>
                        <option selected={currency === PaywallCurrencies.GBP} value={PaywallCurrencies.GBP}>GBP</option>
                    </select>
                </dd>
                <dt>Price:</dt>
                <dd><input type="number" value={price} onChange={(e) => setPrice(parseFloat(e.target.value))}/></dd>
            </dl>
            <button onClick={saveStrikeConfig}>Save</button>
        </div>
    );
}
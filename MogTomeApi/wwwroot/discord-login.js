(function () {
    function jwtDecode(token, options) {
        if (typeof token !== "string") {
            return null;
        }
        options || (options = {});
        const pos = options.header === true ? 0 : 1;
        const part = token.split(".")[pos];
        if (typeof part !== "string") {
            return null;
        }
        let decoded;
        try {
            decoded = base64UrlDecode(part);
        }
        catch (e) {
            return null;
        }
        try {
            return JSON.parse(decoded);
        }
        catch (e) {
            return null;
        }
    }

    function base64UrlDecode(str) {
        let output = str.replace(/-/g, "+").replace(/_/g, "/");
        switch (output.length % 4) {
            case 0:
                break;
            case 2:
                output += "==";
                break;
            case 3:
                output += "=";
                break;
            default:
                throw new Error("base64 string is not of the correct length");
        }
        try {
            return b64DecodeUnicode(output);
        }
        catch (err) {
            return atob(output);
        }
    }

    function b64DecodeUnicode(str) {
        return decodeURIComponent(atob(str).replace(/(.)/g, (m, p) => {
            let code = p.charCodeAt(0).toString(16).toUpperCase();
            if (code.length < 2) {
                code = "0" + code;
            }
            return "%" + code;
        }));
    }

    function placeButton() {
        const authWrapper = document.querySelector(".swagger-ui .auth-wrapper");

        if (!authWrapper) {
            setTimeout(placeButton, 200);
            return;
        }

        if (document.getElementById("discord-login-btn")) {
            return;
        }

        let token = localStorage.getItem("jwt");
        if (token && token.length > 0 && token != 'null') {
            const payload = jwtDecode(token);
            const payloadExpiration = payload.exp;
            const currentTime = Math.floor(Date.now() / 1000);

            if (payloadExpiration < currentTime) {
                localStorage.removeItem("jwt");
                token = null;
            }
            else {
                window.ui.preauthorizeApiKey("Bearer", token);
            }
        }

        const btn = document.createElement("button");
        btn.id = "discord-login-btn";
        btn.innerText = "Login with Discord";
        btn.style = `
            padding: 6px 12px;
            cursor: pointer;
            font-size: 14px;
            margin-right: 10px;
        `;

        btn.onclick = () => {
            const popup = window.open(
                "/auth/discord/swagger-login",
                "discordLogin",
                "width=600,height=800"
            );

            window.addEventListener("message", (event) => {
                if (event.data.type === "discord-jwt") {
                    const jwt = event.data.token;
                    localStorage.setItem("jwt", jwt);
                    window.ui.preauthorizeApiKey("Bearer", jwt);
                }
            });
        };

        // Insert button immediately before the Authorize button
        authWrapper.parentNode.insertBefore(btn, authWrapper);
    }

    placeButton();
})();
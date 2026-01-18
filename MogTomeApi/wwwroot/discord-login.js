(function () {
    function placeButton() {
        const authWrapper = document.querySelector(".swagger-ui .auth-wrapper");

        if (!authWrapper) {
            setTimeout(placeButton, 200);
            return;
        }

        // Prevent duplicates
        if (document.getElementById("discord-login-btn")) {
            return;
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
                    window.ui.preauthorizeApiKey("Bearer", jwt);
                }
            });
        };

        // Insert button immediately before the Authorize button
        authWrapper.parentNode.insertBefore(btn, authWrapper);
    }

    placeButton();
})();
(function () {
    function addDiscordButton() {
        const topbar = document.querySelector(".swagger-ui .topbar");

        if (!topbar) {
            setTimeout(addDiscordButton, 300);
            return;
        }

        // Prevent duplicates
        if (document.getElementById("discord-login-btn")) {
            return;
        }

        // Create a container that pushes content to the right
        const rightContainer = document.createElement("div");
        rightContainer.style.display = "flex";
        rightContainer.style.flex = "1";
        rightContainer.style.justifyContent = "flex-end";
        rightContainer.style.alignItems = "center";

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

        rightContainer.appendChild(btn);

        // Insert the right‑side container into the topbar
        topbar.appendChild(rightContainer);
    }

    addDiscordButton();
})();
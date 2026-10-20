async function pollNotification () {
    const notification = document.getElementById("notificationBadge");
    
    if (notification == null) {
        console.log("Notification polling disabled: User not logged in");
        return;
    }

    try {
        const response = await fetch("/Notification/NotificationCount/", {
            method: "GET",
            headers: {"Accept": "application/json"}
        })

        const data = await response.json();

        if (String(data.count) === notification.textContent)
            return

        if (data.count > 0) {
            notification.textContent = data.count;
            notification.classList.add("active");
        }
        else {
            notification.textContent = "";
            notification.classList.remove("active");
        }
    }
    catch (error) {
        console.log(error);
    }
    finally {
        setTimeout(pollNotification, 10000);
    }
}

void pollNotification();
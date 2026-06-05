


const userConsent: UserConsent = {
    consented: false,
    latti: null,
    longi: null,
    errorMessage: null
}


interface UserConsent
{
    consented: boolean | null;
    latti: number | null,
    longi: number | null,
    errorMessage: string | null
}

/**
 * 
 * @returns Promise<UserConsent> if the geolocation api on the client browser doesnt have access/cant resolve coordinates the consent will be true, but latti and longi will be null consider IP coordinates resolution, null if we polled within the timeoutwindow, set to 30s
 */
function assertGeolocationCompliance(timeoutSeconds: number, maximumAge: number, highAccuracy: boolean = true): Promise<UserConsent | null> {
    return new Promise((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(
            success => {
                resolve({
                    consented: true,
                    latti: success.coords.latitude,
                    longi: success.coords.longitude,
                    errorMessage: null,
                } as UserConsent)
            },
            error => {
                if (error.code === error.PERMISSION_DENIED)
                    resolve({
                        consented: false,
                        latti: null,
                        longi: null,
                        errorMessage: error.message
                    } as UserConsent)
                else if (error.POSITION_UNAVAILABLE === error.code)
                {
                    resolve({
                        consented: true,
                        latti: null,
                        longi: null,
                        errorMessage: "Client can't resolve coordinates"
                    })
                }
                else if (error.code === error.TIMEOUT)
                {
                    return reject(null);
                }
            },
            {
                enableHighAccuracy: highAccuracy, //as high as the user device allows
                maximumAge: maximumAge * 1000,
                timeout: timeoutSeconds * 1000
            } as PositionOptions)
    });
}

(window as any).RustlesGeo = {
    assertGeolocationCompliance
};

(window as any).RustlesChat = {
    scrollToBottom: (element: HTMLElement | null, smooth: boolean): void => {
        if (!element) return;

        element.scrollTo({
            top: element.scrollHeight,
            behavior: smooth ? "smooth" : "auto"
        });
    }
};




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
function assertGeolocationCompliance(timeoutSeconds: number): Promise<UserConsent | null> {
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
                    reject({
                        consented: false,
                        latti: null,
                        longi: null,
                        errorMessage: error.message
                    } as UserConsent)
                else if (error.POSITION_UNAVAILABLE === error.code)
                {
                    reject({
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
                enableHighAccuracy: true, //as high as the user device allows
                maximumAge: 1000 * 5, //5s
                timeout: timeoutSeconds * 1000 //30s the api actually can handle requests with 2,5 seconds but we need to create overhead
            } as PositionOptions)
    });
}

(window as any).RustlesGeo = {
    assertGeolocationCompliance
}

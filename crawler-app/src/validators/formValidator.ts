export const isInvalidEmail = (email: string | undefined): boolean => {
    const validRegularExpression = /^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;

    return !email || email === "" || !validRegularExpression.test(String(email).toLowerCase());
}

export const isInputEmpty = (input: string | undefined | null): boolean => {
    if(input === undefined || input === null)
        return true;

    return String(input).trim() === "";
}
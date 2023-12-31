/** Gets the current Web App's domain.  */
export const domainWithProtocol = (): string => {
  let domain = window.location.origin

  // Fix for IE when origin is not available
  if (!domain)
    domain =
      window.location.protocol +
      "//" +
      window.location.hostname +
      (window.location.port ? ":" + window.location.port : "")

  return domain
}
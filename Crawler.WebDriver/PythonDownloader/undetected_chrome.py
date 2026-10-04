import undetected_chromedriver as uc

options = uc.ChromeOptions()
#options. = False  # headless increases detection


driver = uc.Chrome(headless=False,use_subprocess=False)
driver.get("test uri")


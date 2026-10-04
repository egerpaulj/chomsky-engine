import requests

url = "testurl"

headers = {
    "User-Agent": "Mozilla/5.0"
}

r = requests.get(url, headers=headers)

with open("/home/user/Downloads/worked_file.pdf", "wb") as f:
    f.write(r.content)
    

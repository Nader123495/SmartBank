import urllib.request, http.cookiejar, json

N8N_URL = 'http://localhost:5678'
cookie_jar = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cookie_jar))
login_data = json.dumps({'emailOrLdapLoginId': 'ngattoussa2002@gmail.com', 'password': 'SmartBank2026!'}).encode()
opener.open(urllib.request.Request(N8N_URL + '/rest/login', data=login_data, headers={'Content-Type': 'application/json'}, method='POST'))
cookies_str = '; '.join([c.name + '=' + c.value for c in cookie_jar])
headers = {'Content-Type': 'application/json', 'Cookie': cookies_str}

ex_resp = opener.open(urllib.request.Request(N8N_URL + '/rest/executions/77', headers=headers)).read().decode()
ex_data = json.loads(ex_resp)

# Dump structure to understand it
def explore(obj, prefix='', depth=0):
    if depth > 4:
        return
    if isinstance(obj, dict):
        for k, v in obj.items():
            if isinstance(v, (dict, list)):
                print(prefix + str(k) + ':')
                explore(v, prefix + '  ', depth + 1)
            else:
                val = str(v)[:100]
                print(prefix + str(k) + ': ' + val)
    elif isinstance(obj, list):
        print(prefix + '[list of ' + str(len(obj)) + ']')
        if obj:
            explore(obj[0], prefix + '  [0] ', depth + 1)

explore(ex_data)

#!/usr/bin/env bash

# Emit a file that captures all of the environment variables that are used in the
# rundeployment.sh process. Then a user can source this file to restore those environment 
# variables if their shell session is reset for some reason.

cat > webapp.env << EOF
#!/usr/bin/env bash

$(env | sed -n "s/\(.*_WA=\)\(.*\)/export \1'\2'/p" | sort)
EOF
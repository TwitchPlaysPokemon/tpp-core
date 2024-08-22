# Test that the new executable runs with the existing config.
# Redirect any output just to make sure no json parsing errors or similar can leak secrets.
# Then just replace the executable atomically using mv and restart the service.
testconfig_output=$(./core_update testconfig 2>&1)
testconfig_exitcode=$?
if [[ $testconfig_exitcode == 0 ]]; then
  \cp core core_deploybackup && \
  mv core_update core && \
  systemctl --user restart tpp-dualcore && \
  echo "Successfully deployed!"
  exit 0
elif [[ $testconfig_exitcode == 42 ]]; then
  # 42 = Arbitrary exit code to indicate a semantic error, see also Program.cs
  echo "Failed to run 'testconfig' for new deployment, a semantic error occurred:"
  echo testconfig_output
  exit 1
else
  echo "Failed to run 'testconfig' for new deployment, an uncaught exception occurred."
  echo "The output is suppressed to avoid leaking sensitive data, but this typically means the config file has syntactic or semantic errors."
  exit 1
fi

# Test that the new executable runs with the existing config.
# Redirect any output just to make sure no json parsing errors or similar can leak secrets.
# Then just replace the executable atomically using mv and restart the service.
if ./core_update testconfig >/dev/null 2>&1 ; then
  \cp core core_deploybackup && \
  mv core_update core && \
  systemctl --user restart tpp-dualcore && \
  echo "Successfully deployed!"
else
  echo "Failed to run 'testconfig' for new deployment."
  echo "The output is suppressed to avoid leaking sensitive data, but this typically means the config file has syntactic or semantic errors."
  exit 1
fi
